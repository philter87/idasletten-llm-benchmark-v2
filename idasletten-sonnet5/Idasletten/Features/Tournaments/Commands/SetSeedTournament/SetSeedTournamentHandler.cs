using Idasletten.Shared.Data;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands.SetSeedTournament;

public class SetSeedTournamentHandler(IdaslettenDbContext db) : IRequestHandler<SetSeedTournamentCommand>
{
    public async Task Handle(SetSeedTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found.");

        if (tournament.ParentTournamentId is not null)
        {
            throw new InvalidOperationException("A tournament may be seeded only if it has no parent.");
        }

        tournament.SeedTournamentId = request.SeedTournamentId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
