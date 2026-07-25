using Idasletten.Features.Players.Events;
using Idasletten.Features.Scoring;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

/// <summary>Adds players that already exist (picked from a previous tournament) to this tournament.</summary>
public record AddPlayersFromTournament(
    Guid TournamentId, Guid SourceTournamentId, IReadOnlyList<Guid> UserIds) : IRequest<int>;

public class AddPlayersFromTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<AddPlayersFromTournament, int>
{
    public async Task<int> Handle(AddPlayersFromTournament request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new ArgumentException("Unknown tournament.", nameof(request));

        var added = 0;
        foreach (var userId in request.UserIds.Distinct())
        {
            if (tournament.Players.Any(p => p.UserId == userId))
            {
                continue;
            }

            if (!tournament.HasRoomForMorePlayers(tournament.Players.Count))
            {
                break;
            }

            var player = new TournamentPlayer
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                UserId = userId,
            };

            ScoreEngine.Reset(tournament, player);

            tournament.Players.Add(player);
            db.TournamentPlayers.Add(player);
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new PlayersAddedFromTournament(tournament.Id, request.SourceTournamentId, added),
            cancellationToken);

        return added;
    }
}
