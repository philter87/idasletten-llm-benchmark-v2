using Idasletten.Data;
using Idasletten.Features.Matches.Queries.GetMatchDetail;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetTournamentMatches;

public class GetTournamentMatchesHandler(IdaslettenDbContext db) : IRequestHandler<GetTournamentMatchesQuery, TournamentMatchesResult>
{
    public async Task<TournamentMatchesResult> Handle(GetTournamentMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .Include(m => m.Teams).ThenInclude(t => t.TeamPlayers).ThenInclude(tp => tp.TournamentPlayer).ThenInclude(p => p.User)
            .Include(m => m.Results)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        var planned = matches.Where(m => m.State == MatchState.Planned).Select(ToDto).ToList();
        var completed = matches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.Order).Select(ToDto).ToList();

        return new TournamentMatchesResult(planned, completed);
    }

    private static MatchDetailDto ToDto(TournamentMatch match)
    {
        var resultsByTeam = match.Results.ToDictionary(r => r.TeamId);
        var teams = match.Teams
            .OrderBy(t => t.Number)
            .Select(t =>
            {
                resultsByTeam.TryGetValue(t.Id, out var result);
                return new MatchDetailTeamDto(
                    t.Id,
                    t.Name,
                    t.TeamPlayers.Select(tp => tp.TournamentPlayer.User.UserName ?? string.Empty).ToList(),
                    result?.GoalsWon,
                    result?.GoalsLost);
            })
            .ToList();

        return new MatchDetailDto(match.Id, match.TournamentId, match.Order, match.State, teams);
    }
}
