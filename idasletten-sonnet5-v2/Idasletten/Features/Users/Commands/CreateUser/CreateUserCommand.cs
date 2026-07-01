using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Commands.CreateUser;

public record CreateUserCommand(string Username, string? Name, string? Email = null, string? ImageUrl = null) : IRequest<User>;
