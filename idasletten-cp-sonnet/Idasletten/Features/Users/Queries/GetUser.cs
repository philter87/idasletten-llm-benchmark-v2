using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record UserDto(Guid Id, string Username, string Name, string? Email, string? ImageUrl);

public record GetUserQuery(Guid UserId) : IRequest<UserDto?>;

public sealed class GetUserHandler(AppDbContext db, IMediator mediator) : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly AppDbContext _db = db;

    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == request.UserId)
            .Select(user => new UserDto(
                user.Id,
                user.Username,
                user.Name,
                user.Email,
                user.ImageUrl))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
