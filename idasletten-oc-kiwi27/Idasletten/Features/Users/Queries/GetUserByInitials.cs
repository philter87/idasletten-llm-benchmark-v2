using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record GetUserByInitialsQuery(string Initials) : IRequest<AppUser?>;

public class GetUserByInitialsHandler : IRequestHandler<GetUserByInitialsQuery, AppUser?>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public GetUserByInitialsHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AppUser?> Handle(GetUserByInitialsQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.Initials.Trim().ToUpperInvariant();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalized, cancellationToken);
    }
}
