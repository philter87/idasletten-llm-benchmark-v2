using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

public record CreateUserCommand(string Initials, string? Name = null, string? Email = null) : IRequest<Guid>;

public class UserCreated : INotification
{
    public Guid UserId { get; set; }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IPublisher _publisher;

    public CreateUserHandler(Shared.Data.ApplicationDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Initials.Trim().ToUpperInvariant();
        var existing = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == normalized, cancellationToken);
        if (existing != null) return existing.Id;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = normalized,
            Name = request.Name ?? string.Empty,
            Email = request.Email,
            UserName = normalized,
            NormalizedUserName = normalized.ToUpperInvariant(),
            NormalizedEmail = request.Email?.ToUpperInvariant()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new UserCreated { UserId = user.Id }, cancellationToken);
        return user.Id;
    }
}
