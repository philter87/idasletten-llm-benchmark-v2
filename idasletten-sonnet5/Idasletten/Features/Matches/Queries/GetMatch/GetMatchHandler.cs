using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetMatch;

public class GetMatchHandler(IdaslettenDbContext db) : IRequestHandler<GetMatchQuery, MatchDetailDto?>
{
    public async Task<MatchDetailDto?> Handle(GetMatchQuery request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches
            .Include(m => m.Teams).ThenInclude(t => t.Players)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);
        if (match is null) return null;

        var results = await db.TournamentTeamMatchResults
            .Where(r => r.MatchId == match.Id)
            .ToListAsync(cancellationToken);

        var tournamentPlayerIds = match.Teams.SelectMany(t => t.Players).Select(p => p.TournamentPlayerId).ToList();
        var usernamesByTournamentPlayerId = await (
            from p in db.TournamentPlayers
            join u in db.Users on p.UserId equals u.Id
            where tournamentPlayerIds.Contains(p.Id)
            select new { p.Id, Username = u.UserName! }
        ).ToDictionaryAsync(x => x.Id, x => x.Username, cancellationToken);

        var teams = match.Teams
            .OrderBy(t => t.Number)
            .Select(t =>
            {
                var result = results.FirstOrDefault(r => r.TeamId == t.Id);
                var usernames = t.Players
                    .Select(p => usernamesByTournamentPlayerId.GetValueOrDefault(p.TournamentPlayerId, "?"))
                    .ToList();
                return new MatchTeamDto(t.Number, t.Name, usernames, result?.GoalsWon, result?.GoalsLost);
            })
            .ToList();

        return new MatchDetailDto(match.Id, match.TournamentId, match.State, teams);
    }
}
