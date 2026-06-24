using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreateUserHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<User> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Username == command.Username, ct);
        if (existing != null) return existing;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = command.Username,
            Name = command.Name,
            Email = command.Email,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new UserCreated(user.Id, user.Username), ct);
        return user;
    }
}
