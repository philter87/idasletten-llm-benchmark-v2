using Idasletten.Shared.Data;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands.ArchiveTournament;

public class ArchiveTournamentHandler(AppDbContext db) : IRequestHandler<ArchiveTournamentCommand>
{
    public async Task Handle(ArchiveTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], cancellationToken);
        if (tournament is null) return;
        tournament.IsArchived = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
