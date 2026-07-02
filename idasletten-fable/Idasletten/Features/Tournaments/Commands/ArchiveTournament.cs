using Idasletten.Shared;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record ArchiveTournamentCommand(Guid TournamentId, bool IsArchived) : IRequest;

public record TournamentArchived(Guid TournamentId, bool IsArchived) : INotification;

public class ArchiveTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<ArchiveTournamentCommand>
{
    public async Task Handle(ArchiveTournamentCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        tournament.IsArchived = request.IsArchived;
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new TournamentArchived(tournament.Id, tournament.IsArchived), ct);
    }
}
