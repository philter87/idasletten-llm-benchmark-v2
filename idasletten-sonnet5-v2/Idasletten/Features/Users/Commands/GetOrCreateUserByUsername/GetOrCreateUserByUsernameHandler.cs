using Idasletten.Data;
using Idasletten.Features.Users.Commands.CreateUser;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;

public class GetOrCreateUserByUsernameHandler(IdaslettenDbContext db, ISender sender)
    : IRequestHandler<GetOrCreateUserByUsernameCommand, User>
{
    public async Task<User> Handle(GetOrCreateUserByUsernameCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Username.ToUpperInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        return await sender.Send(new CreateUserCommand(request.Username, request.Name), cancellationToken);
    }
}
