using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Moserware.Skills;

namespace Idasletten.Features.Scoring;

/// <summary>One team's participation in one played match.</summary>
public record TeamOutcome(IReadOnlyList<TournamentPlayer> Players, int GoalsWon, int GoalsLost);

/// <summary>All teams of one played match, in no particular order.</summary>
public record PlayedMatch(IReadOnlyList<TeamOutcome> Teams);

/// <summary>
/// Turns played matches into player scores. It is a pure function of (tournament, players, matches):
/// every write recalculates the whole tournament from scratch, which keeps editing an already played
/// match trivially correct - no incremental undo logic anywhere.
/// </summary>
public static class ScoreEngine
{
    /// <summary>Resets every player and replays all done matches in order.</summary>
    public static void Recalculate(
        Tournament tournament,
        IEnumerable<TournamentPlayer> players,
        IEnumerable<PlayedMatch> matchesInOrder)
    {
        var all = players.ToList();
        foreach (var player in all)
        {
            Reset(tournament, player);
        }

        foreach (var match in matchesInOrder)
        {
            Apply(tournament, match);
        }
    }

    /// <summary>Puts a single player back to the starting state of the tournament's score system.</summary>
    public static void Reset(Tournament tournament, TournamentPlayer player)
    {
        player.WinCount = 0;
        player.LoseCount = 0;
        player.MatchCount = 0;
        player.PointsWon = 0;
        player.PointsLost = 0;
        player.ScoreDiff = 0;
        player.SkillMean = ScoreDefaults.TrueSkillInitialMean;
        player.SkillDeviation = ScoreDefaults.TrueSkillInitialDeviation;
        player.Lives = tournament.ScoreSystem == ScoreSystem.Lives ? ScoreDefaults.StartingLives : 0;
        player.Score = StartScore(tournament.ScoreSystem);
    }

    public static double StartScore(ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => ScoreDefaults.EloStartRating,
        ScoreSystem.TrueSkill => new Rating(
            ScoreDefaults.TrueSkillInitialMean, ScoreDefaults.TrueSkillInitialDeviation).ConservativeRating,
        ScoreSystem.Lives => ScoreDefaults.StartingLives,
        _ => 0,
    };

    /// <summary>Applies one played match on top of the current player state.</summary>
    public static void Apply(Tournament tournament, PlayedMatch match)
    {
        var teams = match.Teams.Where(t => t.Players.Count > 0).ToList();
        if (teams.Count < 2)
        {
            return;
        }

        var scoreBefore = teams.SelectMany(t => t.Players).Distinct()
            .ToDictionary(p => p.Id, p => p.Score);

        ApplyCounters(teams);

        switch (tournament.ScoreSystem)
        {
            case ScoreSystem.Elo:
                ApplyElo(teams);
                break;
            case ScoreSystem.TrueSkill:
                ApplyTrueSkill(teams);
                break;
            case ScoreSystem.Lives:
                ApplyLives(teams);
                break;
            case ScoreSystem.WinCount:
                ApplyWinCount(teams);
                break;
        }

        foreach (var player in teams.SelectMany(t => t.Players).Distinct())
        {
            player.ScoreDiff = player.Score - scoreBefore[player.Id];
        }
    }

    private static void ApplyCounters(List<TeamOutcome> teams)
    {
        var best = teams.Max(t => t.GoalsWon);
        var winnerCount = teams.Count(t => t.GoalsWon == best);

        foreach (var team in teams)
        {
            var isWinner = team.GoalsWon == best && winnerCount == 1;
            var isLoser = team.GoalsWon < best;

            foreach (var player in team.Players)
            {
                player.MatchCount++;
                player.PointsWon += team.GoalsWon;
                player.PointsLost += team.GoalsLost;

                if (isWinner)
                {
                    player.WinCount++;
                }
                else if (isLoser)
                {
                    player.LoseCount++;
                }
            }
        }
    }

    /// <summary>
    /// Classic Elo where a team is rated by the average rating of its players. With more than two
    /// teams every pair is compared and the deltas are averaged over the opponents.
    /// </summary>
    private static void ApplyElo(List<TeamOutcome> teams)
    {
        var ratings = teams.ToDictionary(t => t, t => t.Players.Average(p => p.Score));
        var deltas = teams.ToDictionary(t => t, _ => 0.0);

        foreach (var team in teams)
        {
            foreach (var opponent in teams.Where(o => !ReferenceEquals(o, team)))
            {
                var expected = 1.0 / (1.0 + Math.Pow(10, (ratings[opponent] - ratings[team]) / 400.0));
                var actual = Actual(team.GoalsWon, opponent.GoalsWon);
                deltas[team] += ScoreDefaults.EloKFactor * (actual - expected);
            }
        }

        var opponentCount = teams.Count - 1;
        foreach (var team in teams)
        {
            var delta = deltas[team] / opponentCount;
            foreach (var player in team.Players)
            {
                player.Score += delta;
            }
        }
    }

    private static double Actual(int goals, int opponentGoals) =>
        goals > opponentGoals ? 1.0 : goals == opponentGoals ? 0.5 : 0.0;

    /// <summary>TrueSkill through Moserware.Skills. Score is the conservative rating (mu - 3 sigma).</summary>
    private static void ApplyTrueSkill(List<TeamOutcome> teams)
    {
        var skillTeams = new List<Team<Player<Guid>>>();
        // Player<Guid> uses reference equality, so the instances we create are the dictionary keys.
        var playerLookup = new Dictionary<Player<Guid>, TournamentPlayer>();

        foreach (var team in teams)
        {
            var skillTeam = new Team<Player<Guid>>();
            foreach (var player in team.Players)
            {
                var key = new Player<Guid>(player.Id);
                skillTeam.AddPlayer(key, new Rating(player.SkillMean, player.SkillDeviation));
                playerLookup[key] = player;
            }

            skillTeams.Add(skillTeam);
        }

        // Rank 1 is the winner, equal goals share a rank.
        var ranks = teams
            .Select(team => teams.Count(other => other.GoalsWon > team.GoalsWon) + 1)
            .ToArray();

        var updated = TrueSkillCalculator.CalculateNewRatings(
            GameInfo.DefaultGameInfo, Teams.Concat(skillTeams.ToArray()), ranks);

        foreach (var (key, rating) in updated)
        {
            var player = playerLookup[key];
            player.SkillMean = rating.Mean;
            player.SkillDeviation = rating.StandardDeviation;
            player.Score = rating.ConservativeRating;
        }
    }

    /// <summary>Everyone on a losing team loses one life. The score is the lives left.</summary>
    private static void ApplyLives(List<TeamOutcome> teams)
    {
        var best = teams.Max(t => t.GoalsWon);

        foreach (var team in teams.Where(t => t.GoalsWon < best))
        {
            foreach (var player in team.Players)
            {
                player.Lives = Math.Max(0, player.Lives - 1);
            }
        }

        foreach (var player in teams.SelectMany(t => t.Players))
        {
            player.Score = player.Lives;
        }
    }

    private static void ApplyWinCount(List<TeamOutcome> teams)
    {
        foreach (var player in teams.SelectMany(t => t.Players))
        {
            player.Score = player.WinCount;
        }
    }

    /// <summary>
    /// Scoreboard ordering: best score first, then goal difference, then most wins, then fewest matches.
    /// </summary>
    public static IOrderedEnumerable<TournamentPlayer> Rank(IEnumerable<TournamentPlayer> players) =>
        players
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsDiff)
            .ThenByDescending(p => p.WinCount)
            .ThenBy(p => p.MatchCount);
}
