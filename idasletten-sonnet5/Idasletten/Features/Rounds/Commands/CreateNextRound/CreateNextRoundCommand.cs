using MediatR;

namespace Idasletten.Features.Rounds.Commands.CreateNextRound;

/// Creates the next round as a new tournament linked via ParentTournamentId, copying the
/// parent's settings and its top N players (by parent Score) with scores reset. TopN=null
/// carries over every player from the parent.
public record CreateNextRoundCommand(Guid ParentTournamentId, string Name, int? TopN = null) : IRequest<Guid>;
