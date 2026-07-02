using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

/// <summary>Creates a user by username (usually 3 initials). Idempotent: returns the existing user if the username is taken.</summary>
public record CreateUserCommand(string UserName, string? Name = null, string? Email = null) : IRequest<User>;

public record UserCreated(Guid UserId, string UserName) : INotification;

public class CreateUserHandler(AppDbContext db, IProfileImageProvider imageProvider, IPublisher publisher)
    : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var userName = request.UserName.Trim();
        if (userName.Length == 0)
            throw new ArgumentException("Username must not be empty.", nameof(request));

        var normalized = userName.ToUpperInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, ct);
        if (existing is not null)
            return existing;

        var user = new User
        {
            UserName = userName,
            NormalizedUserName = normalized,
            Name = request.Name?.Trim() ?? userName,
            Email = request.Email?.Trim(),
            NormalizedEmail = request.Email?.Trim().ToUpperInvariant(),
            ImageUrl = await imageProvider.GetImageUrlAsync(userName, request.Email, ct)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new UserCreated(user.Id, user.UserName), ct);
        return user;
    }
}
