using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

/// <summary>Ensures a user with the given username (matched case-insensitively on initials) exists.
/// Returns the existing or new user.</summary>
public record EnsureUserCommand(string Username, string? Name = null) : IRequest<Guid>;

public class EnsureUserHandler(IdaslettenDbContext db, IMediator mediator)
    : IRequestHandler<EnsureUserCommand, Guid>
{
    private readonly IdaslettenDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<Guid> Handle(EnsureUserCommand req, CancellationToken ct)
    {
        var uname = (req.Username ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(uname)) throw new ArgumentException("Username required");
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Username == uname, ct);
        if (existing is not null) return existing.Id;
        var user = new User { Username = uname, Name = req.Name ?? uname };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new UserCreated(user.Id, user.Username), ct);
        return user.Id;
    }
}

public record UserCreated(Guid UserId, string Username) : INotification;