using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record GetUserByUsernameQuery(string Username) : IRequest<User?>;

public class GetUserByUsernameHandler : IRequestHandler<GetUserByUsernameQuery, User?>
{
    private readonly AppDbContext _db;

    public GetUserByUsernameHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> Handle(GetUserByUsernameQuery request, CancellationToken ct)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username.ToUpperInvariant(), ct);
    }
}

public record GetUserByIdQuery(Guid Id) : IRequest<User?>;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, User?>
{
    private readonly AppDbContext _db;

    public GetUserByIdHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        return await _db.Users
            .Include(u => u.TournamentPlayers)
                .ThenInclude(tp => tp.Tournament)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct);
    }
}
