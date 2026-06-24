using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;

namespace Idasletten.Features.Users.Commands;

public record CreateUserCommand(string Username, string Name, string? Email) : IRequest<User>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreateUserHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = new User
        {
            Username = request.Username.ToUpperInvariant(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? request.Username.ToUpperInvariant() : request.Name,
            Email = request.Email
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new UserCreated(user.Id, user.Username), ct);

        return user;
    }
}

public record UserCreated(Guid UserId, string Username) : INotification;

