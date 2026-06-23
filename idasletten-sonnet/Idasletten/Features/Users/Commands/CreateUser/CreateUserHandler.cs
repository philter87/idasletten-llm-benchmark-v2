using Idasletten.Features.Users.Events;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Commands.CreateUser;

public class CreateUserHandler(AppDbContext db, IPublisher publisher) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.ToUpper(),
            Name = request.Name,
            Email = request.Email,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new UserCreated(user.Id, user.Username), cancellationToken);

        return user.Id;
    }
}
