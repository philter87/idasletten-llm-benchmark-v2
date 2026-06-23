using Idasletten.Features.Users.Entities;
using Idasletten.Features.Users.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

public record CreateUserCommand(string Username, string Name, string? Email) : IRequest<Guid>;

public sealed class CreateUserHandler(AppDbContext db, IMediator mediator) : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly AppDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var username = NormalizeRequired(request.Username, nameof(request.Username));
        var name = string.IsNullOrWhiteSpace(request.Name) ? username : request.Name.Trim();
        var email = NormalizeOptional(request.Email);

        var existingUser = await _db.Users
            .FirstOrDefaultAsync(user => user.Username.ToLower() == username.ToLower(), cancellationToken);

        if (existingUser is not null)
        {
            return existingUser.Id;
        }

        var user = new User
        {
            Username = username,
            Name = name,
            Email = email
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await _mediator.Publish(new UserCreated(user.Id, user.Username), cancellationToken);

        return user.Id;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
