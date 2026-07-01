using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands.SetSeedTournament;

public class SetSeedTournamentHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<SetSeedTournamentCommand>
{
    public async Task Handle(SetSeedTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FirstAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament.ParentTournamentId is not null)
        {
            throw new InvalidOperationException("A tournament with a parent cannot be seeded.");
        }

        tournament.SeedTournamentId = request.SeedTournamentId;
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new SeedTournamentSet(tournament.Id, request.SeedTournamentId), cancellationToken);
    }
}
