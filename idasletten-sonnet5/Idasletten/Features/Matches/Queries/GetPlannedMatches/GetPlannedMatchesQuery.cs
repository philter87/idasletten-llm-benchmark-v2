using MediatR;

namespace Idasletten.Features.Matches.Queries.GetPlannedMatches;

public record GetPlannedMatchesQuery(Guid TournamentId, int Take = 5) : IRequest<IReadOnlyList<MatchSummaryDto>>;
