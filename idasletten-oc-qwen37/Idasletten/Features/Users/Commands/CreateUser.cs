using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Users.Commands;

public record CreateUserCommand(string Username, string? Name, string? Email, string? ImageUrl) : IRequest<Guid>;
