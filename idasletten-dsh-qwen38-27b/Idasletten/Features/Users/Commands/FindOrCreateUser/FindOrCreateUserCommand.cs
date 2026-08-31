using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Idasletten.Features.Users.Events;

namespace Idasletten.Features.Users.Commands.FindOrCreateUser;

/// <summary>Find a user by initials (username) or create one (and fire UserCreated).</summary>
public sealed record FindOrCreateUserCommand(string Initials, string? Name = null, string? Email = null) : IRequest<Guid>;

public sealed class FindOrCreateUserCommandHandler : IRequestHandler<FindOrCreateUserCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public FindOrCreateUserCommandHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(FindOrCreateUserCommand request, CancellationToken cancellationToken)
    {
        var username = Normalize(request.Initials);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Username = username,
                Name = string.IsNullOrWhiteSpace(request.Name) ? username : request.Name!.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email!.Trim()
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            await _publisher.Publish(new UserCreated(user.Id), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Name) && user.Name == user.Username)
        {
            // Auto-created from initials earlier; fill in the real name.
            user.Name = request.Name!.Trim();
            await _db.SaveChangesAsync(cancellationToken);
        }

        return user.Id;
    }

    /// <summary>Initials are case-insensitive handles: trimmed, upper-cased.</summary>
    public static string Normalize(string initials)
    {
        var s = (initials ?? string.Empty).Trim().ToUpperInvariant();
        if (s.Length < 2)
            throw new Common.FeatureException("Initials must be at least 2 characters.");
        if (s.Length > 20)
            throw new Common.FeatureException("Initials must be at most 20 characters.");
        return s;
    }
}
