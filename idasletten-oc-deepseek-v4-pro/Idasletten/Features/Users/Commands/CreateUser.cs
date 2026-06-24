using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Commands;

public record CreateUserCommand(string Username, string? Name = null, string? Email = null) : IRequest<User>;

public record UserCreated(Guid UserId, string Username) : INotification;
