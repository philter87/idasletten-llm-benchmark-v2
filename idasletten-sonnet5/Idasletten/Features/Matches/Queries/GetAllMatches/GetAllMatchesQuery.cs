using MediatR;

namespace Idasletten.Features.Matches.Queries.GetAllMatches;

public record GetAllMatchesQuery(Guid TournamentId) : IRequest<AllMatchesDto>;

public record AllMatchesDto(IReadOnlyList<MatchSummaryDto> Planned, IReadOnlyList<MatchSummaryDto> Completed);
