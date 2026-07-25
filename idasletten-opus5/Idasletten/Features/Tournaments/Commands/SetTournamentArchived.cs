using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public record SetTournamentArchived(Guid TournamentId, bool IsArchived) : IRequest<Unit>;

public class SetTournamentArchivedHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<SetTournamentArchived, Unit>
{
    public async Task<Unit> Handle(SetTournamentArchived request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new ArgumentException("Unknown tournament.", nameof(request));

        tournament.IsArchived = request.IsArchived;
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new TournamentArchiveChanged(tournament.Id, tournament.IsArchived), cancellationToken);

        return Unit.Value;
    }
}
