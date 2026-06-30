using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.TournamentPlayers.Commands.RemovePlayerFromTournament;

public class RemovePlayerFromTournamentHandler(IdaslettenDbContext db)
    : IRequestHandler<RemovePlayerFromTournamentCommand>
{
    public async Task Handle(RemovePlayerFromTournamentCommand request, CancellationToken cancellationToken)
    {
        var player = await db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == request.TournamentId && p.UserId == request.UserId, cancellationToken);

        if (player is not null)
        {
            db.TournamentPlayers.Remove(player);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
