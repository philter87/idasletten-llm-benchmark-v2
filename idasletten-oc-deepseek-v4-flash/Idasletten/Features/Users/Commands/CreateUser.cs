using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Events;
using MediatR;

namespace Idasletten.Features.Users.Commands;

public record CreateUserCommand(string Initials, string Name, string? Email = null) : IRequest<Guid>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public CreateUserHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Initials,
            Initials = request.Initials,
            Name = request.Name,
            Email = request.Email
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new UserCreated(user.Id, user.Initials, user.Name), cancellationToken);

        return user.Id;
    }
}
