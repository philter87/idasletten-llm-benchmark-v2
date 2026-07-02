using Idasletten.Features.Matches;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Scoring;

/// <summary>
/// Recalculates all player stats and scores of a tournament from scratch by
/// replaying every Done match in order. Used after editing a completed match.
/// </summary>
public record RecalculateTournamentCommand(Guid TournamentId) : IRequest;

public record TournamentRecalculated(Guid TournamentId) : INotification;

public class RecalculateTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<RecalculateTournamentCommand>
{
    public async Task Handle(RecalculateTournamentCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        var players = await db.TournamentPlayers
            .Where(p => p.TournamentId == tournament.Id)
            .ToListAsync(ct);
        foreach (var player in players)
            ScoringEngine.ResetPlayer(player, tournament.ScoreSystem);

        var teams = await db.TournamentTeams
            .Include(t => t.Players)
            .Where(t => t.TournamentId == tournament.Id)
            .ToListAsync(ct);
        var playersByTeamId = teams.ToDictionary(t => t.Id, t => t.Players);

        var doneMatches = await db.TournamentMatches
            .Include(m => m.Results)
            .Where(m => m.TournamentId == tournament.Id && m.State == MatchState.Done)
            .OrderBy(m => m.Order)
            .ToListAsync(ct);

        foreach (var match in doneMatches)
            ScoringEngine.ApplyMatch(tournament.ScoreSystem, match.Results, playersByTeamId);

        await db.SaveChangesAsync(ct);
        await publisher.Publish(new TournamentRecalculated(tournament.Id), ct);
    }
}
