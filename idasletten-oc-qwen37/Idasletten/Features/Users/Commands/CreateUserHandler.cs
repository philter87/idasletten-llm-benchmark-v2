using Idasletten.Data;
using Idasletten.Features.Users.Events;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreateUserHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (existingUser != null)
        {
            return existingUser.Id;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Name = request.Name,
            Email = request.Email,
            ImageUrl = request.ImageUrl
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new UserCreated(user.Id), cancellationToken);

        return user.Id;
    }
}
