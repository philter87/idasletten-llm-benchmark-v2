using Idasletten.Features.Users.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

/// <summary>
/// Looks up a user by initials and creates one if the initials have not been used before.
/// This is what makes it possible to register a match for people who were never registered.
/// </summary>
public record GetOrCreateUser(string Initials, string? Name = null, string? Email = null)
    : IRequest<User>;

public class GetOrCreateUserHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<GetOrCreateUser, User>
{
    public async Task<User> Handle(GetOrCreateUser request, CancellationToken cancellationToken)
    {
        var initials = Normalize(request.Initials);
        if (string.IsNullOrEmpty(initials))
        {
            throw new ArgumentException("Initials are required to create a user.", nameof(request));
        }

        var existing = await db.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == initials, cancellationToken);

        if (existing is not null)
        {
            // Fill in details we did not know the first time we saw these initials.
            if (string.IsNullOrWhiteSpace(existing.Name) && !string.IsNullOrWhiteSpace(request.Name))
            {
                existing.Name = request.Name.Trim();
                await db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = initials,
            NormalizedUserName = initials,
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            NormalizedEmail = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim().ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new UserCreated(user.Id, user.Initials, user.Email), cancellationToken);

        return user;
    }

    /// <summary>Initials are stored upper case and compared upper case so "abc" and "ABC" is one user.</summary>
    public static string Normalize(string? initials) =>
        (initials ?? string.Empty).Trim().ToUpperInvariant();
}
