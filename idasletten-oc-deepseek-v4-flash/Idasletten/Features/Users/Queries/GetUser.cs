using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record GetUserQuery(Guid Id) : IRequest<User?>;

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
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
    }
}
