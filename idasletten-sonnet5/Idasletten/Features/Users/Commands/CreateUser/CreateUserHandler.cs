using Idasletten.Shared.Data;
using MediatR;

namespace Idasletten.Features.Users.Commands.CreateUser;

public class CreateUserHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            NormalizedUserName = request.Username.ToUpperInvariant(),
            Name = request.Name,
            Email = request.Email,
            NormalizedEmail = request.Email?.ToUpperInvariant(),
            EmailConfirmed = request.Email is not null,
            ImageUrl = request.ImageUrl,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new UserCreated(user.Id, user.UserName), cancellationToken);

        return user.Id;
    }
}
