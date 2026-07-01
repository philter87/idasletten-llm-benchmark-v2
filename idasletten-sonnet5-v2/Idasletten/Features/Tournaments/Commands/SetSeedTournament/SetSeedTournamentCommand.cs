using MediatR;

namespace Idasletten.Features.Tournaments.Commands.SetSeedTournament;

/// <summary>
/// Sets the tournament used to seed/plan matches. A tournament may be seeded only if it has
/// no parent (round tournaments carry their players over already and are never seeded).
/// </summary>
public record SetSeedTournamentCommand(Guid TournamentId, Guid SeedTournamentId) : IRequest;
