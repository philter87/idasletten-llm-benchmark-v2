using Idasletten.Shared;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

/// <summary>Sets the seed tournament. Allowed only when the tournament has no parent.</summary>
public record SetSeedTournamentCommand(Guid TournamentId, Guid SeedTournamentId) : IRequest;

public record SeedTournamentSet(Guid TournamentId, Guid SeedTournamentId) : INotification;

public class SetSeedTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<SetSeedTournamentCommand>
{
    public async Task Handle(SetSeedTournamentCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        if (tournament.ParentTournamentId is not null)
            throw new InvalidOperationException("A tournament may only be seeded if it has no parent tournament.");

        tournament.SeedTournamentId = request.SeedTournamentId;
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new SeedTournamentSet(tournament.Id, request.SeedTournamentId), ct);
    }
}
