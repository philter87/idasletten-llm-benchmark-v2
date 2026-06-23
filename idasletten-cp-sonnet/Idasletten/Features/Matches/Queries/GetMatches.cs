using Idasletten.Features.Matches.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record TeamResultDto(Guid TeamId, string TeamName, int TeamNumber, int Goals, IReadOnlyList<string> PlayerUsernames, bool Won);

public record MatchSummaryDto(Guid Id, Guid TournamentId, int Order, MatchState State, DateTimeOffset? PlayedAt, IReadOnlyList<TeamResultDto> Teams);

public record MatchesDto(IReadOnlyList<MatchSummaryDto> PlannedMatches, IReadOnlyList<MatchSummaryDto> DoneMatches);

public record GetMatchesQuery(Guid TournamentId) : IRequest<MatchesDto>;

public sealed class GetMatchesHandler(AppDbContext db, IMediator mediator) : IRequestHandler<GetMatchesQuery, MatchesDto>
{
    private readonly AppDbContext _db = db;

    public async Task<MatchesDto> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await _db.TournamentMatches
            .AsNoTracking()
            .Where(match => match.TournamentId == request.TournamentId)
            .Include(match => match.TeamResults)
                .ThenInclude(result => result.Team)
                    .ThenInclude(team => team.TeamPlayers)
                        .ThenInclude(teamPlayer => teamPlayer.Player)
                            .ThenInclude(player => player.User)
            .ToListAsync(cancellationToken);

        var plannedMatches = matches
            .Where(match => match.State == MatchState.Planned)
            .OrderBy(match => match.Order)
            .Select(MapMatch)
            .ToList();

        var doneMatches = matches
            .Where(match => match.State == MatchState.Done)
            .OrderByDescending(match => match.PlayedAt)
            .ThenByDescending(match => match.Order)
            .Select(MapMatch)
            .ToList();

        return new MatchesDto(plannedMatches, doneMatches);
    }

    internal static MatchSummaryDto MapMatch(TournamentMatch match)
    {
        var winningGoals = match.TeamResults.Count == 0 ? 0 : match.TeamResults.Max(result => result.GoalsWon);

        return new MatchSummaryDto(
            match.Id,
            match.TournamentId,
            match.Order,
            match.State,
            match.PlayedAt,
            match.TeamResults
                .OrderBy(result => result.Team.Number)
                .Select(result => new TeamResultDto(
                    result.TeamId,
                    result.Team.Name,
                    result.Team.Number,
                    result.GoalsWon,
                    result.Team.TeamPlayers
                        .OrderBy(teamPlayer => teamPlayer.Player.User.Username)
                        .Select(teamPlayer => teamPlayer.Player.User.Username)
                        .ToList(),
                    match.State == MatchState.Done && result.GoalsWon == winningGoals))
                .ToList());
    }
}
