using MediatR;

namespace Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;

/// Adding a player by initials that haven't been used before also creates a User.
/// Idempotent: re-adding a username already in the tournament returns the existing row.
public record AddPlayerToTournamentCommand(Guid TournamentId, string Username, string? Name = null)
    : IRequest<Guid>;
