using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Matches.Queries.GetMatchDetail;

public record GetMatchDetailQuery(Guid MatchId) : IRequest<MatchDetailDto?>;

public record MatchDetailTeamDto(Guid TeamId, string Name, IReadOnlyList<string> PlayerUsernames, int? GoalsWon, int? GoalsLost);

public record MatchDetailDto(Guid MatchId, Guid TournamentId, int Order, MatchState State, IReadOnlyList<MatchDetailTeamDto> Teams);
