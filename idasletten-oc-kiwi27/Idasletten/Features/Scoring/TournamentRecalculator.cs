using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Scoring;

public interface ITournamentRecalculator
{
    Task RecalculateAsync(Guid tournamentId, CancellationToken cancellationToken = default);
}

public class TournamentRecalculator : ITournamentRecalculator
{
    private readonly ApplicationDbContext _db;
    private readonly IScoreCalculatorFactory _factory;

    public TournamentRecalculator(ApplicationDbContext db, IScoreCalculatorFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    public async Task RecalculateAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament == null) return;

        var players = await _db.TournamentPlayers
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync(cancellationToken);

        var calculator = _factory.Create(tournament.ScoreSystem);

        // Reset stats
        foreach (var player in players)
        {
            player.Score = calculator.InitialScore;
            player.WinCount = 0;
            player.MatchCount = 0;
            player.LoseCount = 0;
            player.Lives = LivesCalculator.InitialLives;
            player.PointsWon = 0;
            player.PointsLost = 0;
            player.ScoreDiff = 0;
            player.TrueSkillMean = 25;
            player.TrueSkillStdDev = 25.0 / 3.0;
        }

        var playersByUserId = players.ToDictionary(p => p.UserId);

        var doneMatches = await _db.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId && m.State == MatchState.Done)
            .OrderBy(m => m.CompletedAt)
            .ThenBy(m => m.Order)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Members)
            .ToListAsync(cancellationToken);

        foreach (var match in doneMatches)
        {
            // Ensure all members exist as tournament players (defensive)
            foreach (var team in match.Teams)
            {
                foreach (var member in team.Members)
                {
                    if (!playersByUserId.ContainsKey(member.UserId))
                    {
                        // This can happen if a user was added via match but not a formal tournament player
                        playersByUserId[member.UserId] = member;
                    }
                }
            }

            calculator.ApplyMatch(tournament, playersByUserId, match);
        }

        _db.TournamentPlayers.UpdateRange(players);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
