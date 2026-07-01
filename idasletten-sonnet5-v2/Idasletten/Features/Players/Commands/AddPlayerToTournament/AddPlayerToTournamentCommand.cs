using MediatR;

namespace Idasletten.Features.Players.Commands.AddPlayerToTournament;

/// <summary>
/// Adds a player to a tournament by initials, auto-creating the User if the initials
/// haven't been used before. Idempotent: adding an already-registered player is a no-op
/// that just returns their existing TournamentPlayer.
/// </summary>
public record AddPlayerToTournamentCommand(Guid TournamentId, string Username, string? Name = null) : IRequest<Guid>;
