using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public class GetUserHandler : IRequestHandler<GetUserQuery, User?>
{
    private readonly AppDbContext _db;

    public GetUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .Include(u => u.TournamentPlayers)
            .ThenInclude(tp => tp.Tournament)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
    }
}
