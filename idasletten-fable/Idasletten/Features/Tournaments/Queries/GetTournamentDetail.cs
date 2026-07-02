using Idasletten.Features.Matches;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournamentDetailQuery(Guid TournamentId) : IRequest<TournamentDetailResult?>;

public record TournamentDetailResult(
    Tournament Tournament,
    List<TournamentPlayer> Scoreboard,
    List<TournamentMatch> NextPlannedMatches,
    List<TournamentMatch> RecentPlayedMatches,
    List<Tournament> Rounds);

public class GetTournamentDetailHandler(AppDbContext db) : IRequestHandler<GetTournamentDetailQuery, TournamentDetailResult?>
{
    public async Task<TournamentDetailResult?> Handle(GetTournamentDetailQuery request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, ct);
        if (tournament is null)
            return null;

        var scoreboard = await db.TournamentPlayers.AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id)
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ThenByDescending(p => p.WinCount)
            .ToListAsync(ct);

        var planned = await MatchesWithTeams(tournament.Id)
            .Where(m => m.State == MatchState.Planned)
            .OrderBy(m => m.Order)
            .Take(5)
            .ToListAsync(ct);

        var played = await MatchesWithTeams(tournament.Id)
            .Where(m => m.State == MatchState.Done)
            .OrderByDescending(m => m.PlayedAt)
            .Take(5)
            .ToListAsync(ct);

        var rounds = await db.Tournaments.AsNoTracking()
            .Where(t => t.ParentTournamentId == tournament.Id)
            .OrderBy(t => t.RoundNumber)
            .ToListAsync(ct);

        return new TournamentDetailResult(tournament, scoreboard, planned, played, rounds);
    }

    private IQueryable<TournamentMatch> MatchesWithTeams(Guid tournamentId) =>
        db.TournamentMatches.AsNoTracking()
            .Include(m => m.Results)
            .ThenInclude(r => r.Team)
            .ThenInclude(t => t.Players)
            .ThenInclude(p => p.User)
            .Where(m => m.TournamentId == tournamentId);
}
