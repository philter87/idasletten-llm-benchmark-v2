using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries.GetUser;

public class GetUserHandler(AppDbContext db) : IRequestHandler<GetUserQuery, User?>
{
    public Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => db.Users
             .Include(u => u.TournamentPlayers).ThenInclude(tp => tp.Tournament)
             .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
}
