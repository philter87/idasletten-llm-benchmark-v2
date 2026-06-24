using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record FindUserByInitialsQuery(string Initials) : IRequest<User?>;

public class FindUserByInitialsHandler : IRequestHandler<FindUserByInitialsQuery, User?>
{
    private readonly AppDbContext _db;

    public FindUserByInitialsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> Handle(FindUserByInitialsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Initials == request.Initials, cancellationToken);
    }
}
