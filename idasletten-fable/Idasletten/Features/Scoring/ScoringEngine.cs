using Idasletten.Features.Matches;
using Idasletten.Features.Tournaments;
using Moserware.Skills;

namespace Idasletten.Features.Scoring;

/// <summary>
/// Applies a completed match to the participating players' stats and Score,
/// according to the tournament's ScoreSystem.
/// </summary>
public static class ScoringEngine
{
    public const double EloInitialScore = 1200;
    public const double EloKFactor = 32;

    public static double InitialScore(ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => EloInitialScore,
        ScoreSystem.TrueSkill => ConservativeTrueSkill(GameInfo.DefaultGameInfo.DefaultRating),
        ScoreSystem.Lives => TournamentPlayer.DefaultLives,
        ScoreSystem.WinCount => 0,
        _ => 0
    };

    /// <summary>Resets a player to the state they had before playing any match.</summary>
    public static void ResetPlayer(TournamentPlayer player, ScoreSystem system)
    {
        player.Score = InitialScore(system);
        player.ScoreDiff = 0;
        player.WinCount = 0;
        player.LoseCount = 0;
        player.MatchCount = 0;
        player.PointsWon = 0;
        player.PointsLost = 0;
        player.Lives = system == ScoreSystem.Lives ? TournamentPlayer.DefaultLives : 0;
        player.TrueSkillMean = GameInfo.DefaultGameInfo.DefaultRating.Mean;
        player.TrueSkillStdDev = GameInfo.DefaultGameInfo.DefaultRating.StandardDeviation;
    }

    /// <summary>
    /// Updates stats and scores for all players in a done match.
    /// Teams are ranked by their goals; supports two or more teams.
    /// </summary>
    public static void ApplyMatch(
        ScoreSystem system,
        IReadOnlyList<TournamentTeamMatchResult> results,
        IReadOnlyDictionary<Guid, List<TournamentPlayer>> playersByTeamId)
    {
        var ranked = results.OrderByDescending(r => r.GoalsWon).ToList();
        var maxGoals = ranked[0].GoalsWon;

        var oldScores = playersByTeamId.Values.SelectMany(p => p)
            .Distinct()
            .ToDictionary(p => p.Id, p => p.Score);

        foreach (var result in results)
        {
            var isWinner = result.GoalsWon == maxGoals && ranked.Count(r => r.GoalsWon == maxGoals) == 1;
            var isLoser = result.GoalsWon < maxGoals;
            foreach (var player in playersByTeamId[result.TeamId])
            {
                player.MatchCount++;
                player.PointsWon += result.GoalsWon;
                player.PointsLost += result.GoalsLost;
                if (isWinner) player.WinCount++;
                if (isLoser) player.LoseCount++;
            }
        }

        switch (system)
        {
            case ScoreSystem.Elo:
                ApplyElo(results, playersByTeamId);
                break;
            case ScoreSystem.TrueSkill:
                ApplyTrueSkill(results, playersByTeamId);
                break;
            case ScoreSystem.Lives:
                ApplyLives(results, playersByTeamId, maxGoals);
                break;
            case ScoreSystem.WinCount:
                foreach (var players in playersByTeamId.Values)
                    foreach (var player in players.Distinct())
                        player.Score = player.WinCount;
                break;
        }

        foreach (var player in playersByTeamId.Values.SelectMany(p => p).Distinct())
            player.ScoreDiff = player.Score - oldScores[player.Id];
    }

    private static void ApplyElo(
        IReadOnlyList<TournamentTeamMatchResult> results,
        IReadOnlyDictionary<Guid, List<TournamentPlayer>> playersByTeamId)
    {
        // Team rating is the average of its players' scores. With more than two
        // teams, Elo is applied pairwise between every pair of teams.
        var teamRatings = results.ToDictionary(
            r => r.TeamId,
            r => playersByTeamId[r.TeamId].Average(p => p.Score));

        var deltas = results.ToDictionary(r => r.TeamId, _ => 0.0);

        for (var i = 0; i < results.Count; i++)
        {
            for (var j = i + 1; j < results.Count; j++)
            {
                var a = results[i];
                var b = results[j];
                var expectedA = 1.0 / (1.0 + Math.Pow(10, (teamRatings[b.TeamId] - teamRatings[a.TeamId]) / 400.0));
                var actualA = a.GoalsWon > b.GoalsWon ? 1.0 : a.GoalsWon < b.GoalsWon ? 0.0 : 0.5;
                var delta = EloKFactor * (actualA - expectedA);
                deltas[a.TeamId] += delta;
                deltas[b.TeamId] -= delta;
            }
        }

        foreach (var result in results)
            foreach (var player in playersByTeamId[result.TeamId])
                player.Score += deltas[result.TeamId];
    }

    private static void ApplyTrueSkill(
        IReadOnlyList<TournamentTeamMatchResult> results,
        IReadOnlyDictionary<Guid, List<TournamentPlayer>> playersByTeamId)
    {
        var gameInfo = GameInfo.DefaultGameInfo;
        var teams = new List<Team>();
        var playerLookup = new Dictionary<Guid, TournamentPlayer>();

        foreach (var result in results)
        {
            var team = new Team();
            foreach (var player in playersByTeamId[result.TeamId])
            {
                playerLookup[player.Id] = player;
                team.AddPlayer(
                    new Player(player.Id),
                    new Rating(player.TrueSkillMean, player.TrueSkillStdDev));
            }
            teams.Add(team);
        }

        // Rank 1 is best. Teams with equal goals share a rank (draw).
        var ordered = results.OrderByDescending(r => r.GoalsWon).ToList();
        var ranks = new int[results.Count];
        for (var i = 0; i < results.Count; i++)
        {
            var position = ordered.FindIndex(r => r.GoalsWon == results[i].GoalsWon);
            ranks[i] = position + 1;
        }

        var newRatings = TrueSkillCalculator.CalculateNewRatings(
            gameInfo, teams.Select(t => t.AsDictionary()), ranks);

        foreach (var (moserwarePlayer, rating) in newRatings)
        {
            var player = playerLookup[(Guid)moserwarePlayer.Id];
            player.TrueSkillMean = rating.Mean;
            player.TrueSkillStdDev = rating.StandardDeviation;
            player.Score = ConservativeTrueSkill(rating);
        }
    }

    private static void ApplyLives(
        IReadOnlyList<TournamentTeamMatchResult> results,
        IReadOnlyDictionary<Guid, List<TournamentPlayer>> playersByTeamId,
        int maxGoals)
    {
        foreach (var result in results)
        {
            var lostMatch = result.GoalsWon < maxGoals;
            foreach (var player in playersByTeamId[result.TeamId])
            {
                if (lostMatch && player.Lives > 0) player.Lives--;
                player.Score = player.Lives;
            }
        }
    }

    private static double ConservativeTrueSkill(Rating rating) =>
        Math.Round(rating.ConservativeRating, 2);
}
