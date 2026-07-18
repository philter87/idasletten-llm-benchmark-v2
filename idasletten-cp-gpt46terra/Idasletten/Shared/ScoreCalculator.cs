using Microsoft.EntityFrameworkCore;
using Moserware.Skills;

namespace Idasletten.Shared;

public class ScoreCalculator(IdaslettenDbContext db)
{
    public async Task RecalculateAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(x => x.Players)
            .FirstAsync(x => x.Id == tournamentId, cancellationToken);
        foreach (var player in tournament.Players)
        {
            player.Score = tournament.ScoreSystem == ScoreSystem.TrueSkill ? 25 : 1000;
            player.ScoreDiff = 0;
            player.WinCount = player.LoseCount = player.MatchCount = 0;
            player.PointsWon = player.PointsLost = 0;
            player.Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : null;
        }

        var matches = await db.TournamentMatches
            .Where(x => x.TournamentId == tournamentId && x.State == MatchState.Done)
            .Include(x => x.Results).ThenInclude(x => x.Team).ThenInclude(x => x.Players)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        foreach (var match in matches)
            ApplyMatch(tournament, match);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyMatch(Tournament tournament, TournamentMatch match)
    {
        if (match.Results.Count < 2)
            return;
        var ordered = match.Results.OrderByDescending(x => x.GoalsWon).ToList();
        var winner = ordered[0];
        var loser = ordered[1];
        var winnerPlayers = winner.Team.Players.Select(x => x.TournamentPlayer).ToList();
        var loserPlayers = loser.Team.Players.Select(x => x.TournamentPlayer).ToList();
        var expected = 1d / (1d + Math.Pow(10, (loserPlayers.Average(x => x.Score) - winnerPlayers.Average(x => x.Score)) / 400d));
        var swing = 32 * (1 - expected);
        Dictionary<TournamentPlayer, double>? trueSkillRatings = null;
        if (tournament.ScoreSystem == ScoreSystem.TrueSkill)
            trueSkillRatings = CalculateTrueSkill(winnerPlayers, loserPlayers);

        foreach (var player in winnerPlayers)
        {
            var previousScore = player.Score;
            Update(player, winner.GoalsWon, winner.GoalsLost, true, tournament.ScoreSystem, swing);
            if (trueSkillRatings is not null)
            {
                player.Score = trueSkillRatings[player];
                player.ScoreDiff = player.Score - previousScore;
            }
        }
        foreach (var player in loserPlayers)
        {
            var previousScore = player.Score;
            Update(player, loser.GoalsWon, loser.GoalsLost, false, tournament.ScoreSystem, swing);
            if (trueSkillRatings is not null)
            {
                player.Score = trueSkillRatings[player];
                player.ScoreDiff = player.Score - previousScore;
            }
        }
    }

    private static Dictionary<TournamentPlayer, double> CalculateTrueSkill(
        IEnumerable<TournamentPlayer> winners,
        IEnumerable<TournamentPlayer> losers)
    {
        var gameInfo = GameInfo.DefaultGameInfo;
        var players = new Dictionary<Player, TournamentPlayer>();
        var winningTeam = new Team();
        var losingTeam = new Team();
        foreach (var player in winners)
        {
            var skillPlayer = new Player(player.Id);
            players.Add(skillPlayer, player);
            winningTeam.AddPlayer(skillPlayer, new Rating(player.Score, gameInfo.DefaultRating.StandardDeviation));
        }
        foreach (var player in losers)
        {
            var skillPlayer = new Player(player.Id);
            players.Add(skillPlayer, player);
            losingTeam.AddPlayer(skillPlayer, new Rating(player.Score, gameInfo.DefaultRating.StandardDeviation));
        }
        var ratings = TrueSkillCalculator.CalculateNewRatings(gameInfo, Teams.Concat(winningTeam, losingTeam), 1, 2);
        return players.ToDictionary(x => x.Value, x => ratings[x.Key].ConservativeRating);
    }

    private static void Update(TournamentPlayer player, int won, int lost, bool didWin, ScoreSystem system, double swing)
    {
        player.MatchCount++;
        player.PointsWon += won;
        player.PointsLost += lost;
        if (didWin) player.WinCount++; else player.LoseCount++;
        var diff = system switch
        {
            ScoreSystem.Elo => didWin ? swing : -swing,
            ScoreSystem.TrueSkill => 0,
            ScoreSystem.Lives => 0,
            ScoreSystem.WinCount => didWin ? 1 : 0,
            _ => 0
        };
        player.ScoreDiff = diff;
        if (system == ScoreSystem.Lives && !didWin)
            player.Lives = Math.Max(0, (player.Lives ?? 3) - 1);
        if (system == ScoreSystem.WinCount)
            player.Score = player.WinCount;
        else
            player.Score += diff;
    }
}
