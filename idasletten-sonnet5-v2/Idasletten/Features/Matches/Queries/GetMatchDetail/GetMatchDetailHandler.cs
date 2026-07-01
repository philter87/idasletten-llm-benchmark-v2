using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetMatchDetail;

public class GetMatchDetailHandler(IdaslettenDbContext db) : IRequestHandler<GetMatchDetailQuery, MatchDetailDto?>
{
    public async Task<MatchDetailDto?> Handle(GetMatchDetailQuery request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches
            .Include(m => m.Teams).ThenInclude(t => t.TeamPlayers).ThenInclude(tp => tp.TournamentPlayer).ThenInclude(p => p.User)
            .Include(m => m.Results)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match is null)
        {
            return null;
        }

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
