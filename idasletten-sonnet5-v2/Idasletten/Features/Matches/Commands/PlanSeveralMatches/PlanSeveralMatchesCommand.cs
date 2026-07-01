using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Matches.Commands.PlanSeveralMatches;

/// <summary>
/// Bulk-plans matches for a tournament.
/// - GamesPerPlayer determines how many matches get created (players.Count * GamesPerPlayer
///   player-slots, divided into matches of TeamSize * 2 slots each).
/// - FixedTeams: when true, teams are formed once and repeat across rounds (only the
///   opponent pairing rotates); when false, teams are re-formed every round.
/// - SeedingType Random shuffles players; Equality pairs best-with-worst (1+N, 2+(N-1), ...)
///   using the seed tournament's ranking; Fair splits the ranked field into a top and
///   bottom half and pairs 1+(N/2+1), 2+(N/2+2), etc.
/// </summary>
public record PlanSeveralMatchesCommand(
    Guid TournamentId,
    Guid? SeedTournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType) : IRequest<IReadOnlyList<Guid>>;
