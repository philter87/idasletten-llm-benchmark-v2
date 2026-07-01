using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;

/// <summary>
/// Looks up a User by initials, creating one (via CreateUserCommand) if it doesn't exist yet.
/// Used everywhere players are entered by initials: create-match, add-player dialogs, etc.
/// </summary>
public record GetOrCreateUserByUsernameCommand(string Username, string? Name = null) : IRequest<User>;
