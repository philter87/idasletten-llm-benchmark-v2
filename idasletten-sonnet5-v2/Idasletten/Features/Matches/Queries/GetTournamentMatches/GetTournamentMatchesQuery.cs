using Idasletten.Features.Matches.Queries.GetMatchDetail;
using MediatR;

namespace Idasletten.Features.Matches.Queries.GetTournamentMatches;

public record GetTournamentMatchesQuery(Guid TournamentId) : IRequest<TournamentMatchesResult>;

public record TournamentMatchesResult(
    IReadOnlyList<MatchDetailDto> PlannedMatches,
    IReadOnlyList<MatchDetailDto> CompletedMatches);
