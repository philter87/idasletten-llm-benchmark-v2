using Idasletten.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournamentDetail;

public class GetTournamentDetailHandler(IdaslettenDbContext db)
    : IRequestHandler<GetTournamentDetailQuery, TournamentDetailResult?>
{
    public async Task<TournamentDetailResult?> Handle(GetTournamentDetailQuery request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players).ThenInclude(p => p.User)
            .Include(t => t.Matches).ThenInclude(m => m.Teams).ThenInclude(team => team.TeamPlayers).ThenInclude(tp => tp.TournamentPlayer).ThenInclude(p => p.User)
            .Include(t => t.Matches).ThenInclude(m => m.Results)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return null;
        }

        var scoreboard = tournament.Players
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => tournament.ScoreSystem == ScoreSystem.WinCount ? p.PointsWon - p.PointsLost : 0)
            .Select(p => new TournamentDetailPlayerDto(
                p.Id,
                p.UserId,
                p.User.UserName ?? string.Empty,
                p.User.Name,
                p.User.ImageUrl,
                p.Score,
                p.ScoreDiff,
                p.WinCount,
                p.LoseCount,
                p.MatchCount,
                p.Lives,
                p.PointsWon,
                p.PointsLost))
            .ToList();

        var plannedMatches = tournament.Matches
            .Where(m => m.State == MatchState.Planned)
            .OrderBy(m => m.Order)
            .Take(5)
            .Select(ToDto)
            .ToList();

        var recentMatches = tournament.Matches
            .Where(m => m.State == MatchState.Done)
            .OrderByDescending(m => m.Order)
            .Take(5)
            .Select(ToDto)
            .ToList();

        return new TournamentDetailResult(
            tournament.Id,
            tournament.Name,
            tournament.TeamSize,
            tournament.PointsToWin,
            tournament.ScoreSystem,
            tournament.MaxPlayerCount,
            tournament.IsArchived,
            tournament.IsPublic,
            tournament.SeedTournamentId,
            tournament.ParentTournamentId,
            tournament.RoundNumber,
            scoreboard,
            plannedMatches,
            recentMatches);
    }

    private static TournamentDetailMatchDto ToDto(TournamentMatch match)
    {
        var resultsByTeam = match.Results.ToDictionary(r => r.TeamId);
        var teams = match.Teams
            .OrderBy(t => t.Number)
            .Select(t =>
            {
                resultsByTeam.TryGetValue(t.Id, out var result);
                return new TournamentDetailMatchTeamDto(
                    t.Id,
                    t.Name,
                    t.TeamPlayers.Select(tp => tp.TournamentPlayer.User.UserName ?? string.Empty).ToList(),
                    result?.GoalsWon,
                    result?.GoalsLost);
            })
            .ToList();

        return new TournamentDetailMatchDto(match.Id, match.Order, match.State, teams);
    }
}
