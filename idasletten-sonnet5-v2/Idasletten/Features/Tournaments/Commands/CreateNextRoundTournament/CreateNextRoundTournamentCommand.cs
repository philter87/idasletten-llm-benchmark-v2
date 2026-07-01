using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateNextRoundTournament;

/// <summary>
/// Starts a new round that continues from an existing tournament: copies its configuration
/// and top players (by Score), with scores reset, into a new tournament linked via
/// ParentTournamentId. A tournament may only be seeded (SeedTournamentId) if it has no parent,
/// so round tournaments are never themselves seedable.
/// </summary>
public record CreateNextRoundTournamentCommand(Guid ParentTournamentId, int? TopPlayerCount, string? Name) : IRequest<Guid>;
