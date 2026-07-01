using MediatR;

namespace Idasletten.Features.Players.Commands.AddExistingUserToTournament;

/// <summary>
/// Adds an already-known User to a tournament — used by the "select from list" checkbox
/// dialog and the seed-tournament "+ / -" player picker.
/// </summary>
public record AddExistingUserToTournamentCommand(Guid TournamentId, Guid UserId) : IRequest<Guid>;
