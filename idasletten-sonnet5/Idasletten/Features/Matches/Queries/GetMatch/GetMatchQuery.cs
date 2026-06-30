using MediatR;

namespace Idasletten.Features.Matches.Queries.GetMatch;

public record GetMatchQuery(Guid MatchId) : IRequest<MatchDetailDto?>;

public record MatchDetailDto(
    Guid Id,
    Guid TournamentId,
    MatchState State,
    IReadOnlyList<MatchTeamDto> Teams);

public record MatchTeamDto(int Number, string Name, IReadOnlyList<string> PlayerUsernames, int? GoalsWon, int? GoalsLost);
