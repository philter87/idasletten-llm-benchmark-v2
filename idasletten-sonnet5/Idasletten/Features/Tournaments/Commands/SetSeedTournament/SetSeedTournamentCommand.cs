using MediatR;

namespace Idasletten.Features.Tournaments.Commands.SetSeedTournament;

/// A tournament may be seeded only if it has no parent.
public record SetSeedTournamentCommand(Guid TournamentId, Guid SeedTournamentId) : IRequest;
