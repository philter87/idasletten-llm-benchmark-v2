using Idasletten.Features.Users.Commands.CreateUser;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;

public class GetOrCreateUserByUsernameHandler(IdaslettenDbContext db, ISender sender)
    : IRequestHandler<GetOrCreateUserByUsernameCommand, Guid>
{
    public async Task<Guid> Handle(GetOrCreateUserByUsernameCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Username.Trim().ToUpperInvariant();

        var existingId = await db.Users
            .Where(u => u.NormalizedUserName == normalized)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is not null)
        {
            return existingId.Value;
        }

        return await sender.Send(new CreateUserCommand(request.Username.Trim(), request.Username.Trim()), cancellationToken);
    }
}
