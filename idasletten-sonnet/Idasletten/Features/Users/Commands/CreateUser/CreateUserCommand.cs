using MediatR;

namespace Idasletten.Features.Users.Commands.CreateUser;

public record CreateUserCommand(string Username, string Name, string? Email = null) : IRequest<Guid>;
