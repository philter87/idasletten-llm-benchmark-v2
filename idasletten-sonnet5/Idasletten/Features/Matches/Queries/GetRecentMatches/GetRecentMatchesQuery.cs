using MediatR;

namespace Idasletten.Features.Matches.Queries.GetRecentMatches;

public record GetRecentMatchesQuery(Guid TournamentId, int Take = 5) : IRequest<IReadOnlyList<MatchSummaryDto>>;
