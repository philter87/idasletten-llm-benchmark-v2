using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

/// <summary>One match, used to pre-fill the create-match page and to show a played match.</summary>
public record GetMatch(Guid TournamentId, Guid MatchId) : IRequest<MatchRow?>;

public class GetMatchHandler(AppDbContext db) : IRequestHandler<GetMatch, MatchRow?>
{
    public async Task<MatchRow?> Handle(GetMatch request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches
            .AsNoTracking()
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(p => p.TournamentPlayer)
                            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(
                m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, cancellationToken);

        return match is null ? null : MatchRowMapper.Map(match);
    }
}

internal static class MatchRowMapper
{
    public static MatchRow Map(TournamentMatch match) => new(
        match.Id,
        match.Order,
        match.State,
        match.PlayedUtc,
        match.Results
            .OrderBy(result => result.Team.Number)
            .Select(result => new MatchTeamRow(
                result.TeamId,
                result.Team.Name,
                result.Team.Number,
                result.GoalsWon,
                result.Team.Players
                    .Select(player => new MatchPlayerRow(
                        player.TournamentPlayer.UserId,
                        player.TournamentPlayer.User.Initials,
                        player.TournamentPlayer.User.DisplayName,
                        player.TournamentPlayer.User.ImageUrl))
                    .OrderBy(player => player.Initials)
                    .ToList()))
            .ToList());
}
