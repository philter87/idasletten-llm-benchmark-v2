using Idasletten.Features.Users.Queries;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public class GetUserHandler :
    IRequestHandler<GetUserByUsernameQuery, User?>,
    IRequestHandler<GetUserByIdQuery, User?>
{
    private readonly AppDbContext _db;

    public GetUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> Handle(GetUserByUsernameQuery query, CancellationToken ct)
    {
        return await _db.Users
            .Include(u => u.TournamentPlayers).ThenInclude(tp => tp.Tournament)
            .FirstOrDefaultAsync(u => u.Username == query.Username, ct);
    }

    public async Task<User?> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        return await _db.Users
            .Include(u => u.TournamentPlayers).ThenInclude(tp => tp.Tournament)
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
    }
}
