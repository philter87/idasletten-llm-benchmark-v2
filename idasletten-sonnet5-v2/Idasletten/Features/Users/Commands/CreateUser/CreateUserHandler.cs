using Idasletten.Data;
using Idasletten.Shared.Auth;
using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Commands.CreateUser;

public class CreateUserHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var username = await UsernameGenerator.EnsureUniqueAsync(db, request.Username, cancellationToken);
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? username : request.Name,
            Email = request.Email,
            NormalizedEmail = request.Email?.ToUpperInvariant(),
            ImageUrl = request.ImageUrl,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new UserCreated(user.Id), cancellationToken);

        return user;
    }
}
