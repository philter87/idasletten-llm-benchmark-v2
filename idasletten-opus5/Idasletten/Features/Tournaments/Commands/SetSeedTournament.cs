using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

/// <summary>Points a tournament at the previous tournament its players and matches are planned from.</summary>
public record SetSeedTournament(Guid TournamentId, Guid SeedTournamentId) : IRequest<Unit>;

public class SetSeedTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<SetSeedTournament, Unit>
{
    public async Task<Unit> Handle(SetSeedTournament request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new ArgumentException("Unknown tournament.", nameof(request));

        if (!tournament.CanBeSeeded)
        {
            throw new InvalidOperationException(
                "A tournament that continues from a parent tournament cannot be seeded.");
        }

        if (request.SeedTournamentId == request.TournamentId)
        {
            throw new InvalidOperationException("A tournament cannot seed itself.");
        }

        tournament.SeedTournamentId = request.SeedTournamentId;
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new SeedTournamentSet(tournament.Id, request.SeedTournamentId), cancellationToken);

        return Unit.Value;
    }
}
