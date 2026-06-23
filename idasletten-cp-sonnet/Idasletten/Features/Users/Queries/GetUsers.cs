using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record GetUsersQuery() : IRequest<IReadOnlyList<UserDto>>;

public sealed class GetUsersHandler(AppDbContext db, IMediator mediator) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new UserDto(
                user.Id,
                user.Username,
                user.Name,
                user.Email,
                user.ImageUrl))
            .ToListAsync(cancellationToken);
    }
}
