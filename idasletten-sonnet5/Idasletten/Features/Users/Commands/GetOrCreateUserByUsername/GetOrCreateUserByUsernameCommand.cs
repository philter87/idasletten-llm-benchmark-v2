using MediatR;

namespace Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;

/// Used wherever initials are typed in without first selecting an existing player
/// (create-match, add-player dialogs): looks up a User by username (initials), creating one
/// if it doesn't exist yet.
public record GetOrCreateUserByUsernameCommand(string Username) : IRequest<Guid>;
