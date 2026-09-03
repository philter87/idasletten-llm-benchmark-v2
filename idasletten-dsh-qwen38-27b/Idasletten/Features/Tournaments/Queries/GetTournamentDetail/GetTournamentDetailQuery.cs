using Idasletten.Data;
using Idasletten.Features.Tournaments;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournamentDetail;

public sealed record GetTournamentDetailQuery(Guid TournamentId) : IRequest<TournamentDetailDto?>;

public sealed class GetTournamentDetailQueryHandler : IRequestHandler<GetTournamentDetailQuery, TournamentDetailDto?>
{
    private readonly AppDbContext _db;

    public GetTournamentDetailQueryHandler(AppDbContext db) => _db = db;

    public async Task<TournamentDetailDto?> Handle(GetTournamentDetailQuery request, CancellationToken cancellationToken)
    {
        var t = await _db.Tournaments
            .Include(x => x.SeedTournament)
            .Include(x => x.ParentTournament)
            .FirstOrDefaultAsync(x => x.Id == request.TournamentId, cancellationToken);
        if (t is null) return null;

        var players = await _db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == t.Id)
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ThenBy(p => p.PointsLost)
            .ThenBy(p => p.User.Username)
            .Select(p => new PlayerRowDto(
                p.Id, p.UserId, p.User.Username, p.User.Name, p.User.Email, p.User.ImageUrl,
                p.Score, p.ScoreDiff, p.WinCount, p.LoseCount, p.MatchCount,
                p.PointsWon, p.PointsLost, p.Lives))
            .ToListAsync(cancellationToken);

        var planned = await BuildMatchesAsync(_db, t.Id, MatchState.Planned, 5, latestFirst: false, cancellationToken);
        var played = await BuildMatchesAsync(_db, t.Id, MatchState.Done, 5, latestFirst: true, cancellationToken);

        return new TournamentDetailDto(
            t.Id, t.Name, t.ScoreSystem, t.TeamSize, t.PointsToWin,
            t.MaxPlayerCount, t.IsPublic, t.IsArchived, t.ParentTournamentId is not null, t.RoundNumber,
            t.ParentTournament?.Name, t.RoundNumber,
            t.SeedTournamentId, t.SeedTournament?.Name,
            players.Count, players, planned, played);
    }

    internal static async Task<IReadOnlyList<MatchSummaryDto>> BuildMatchesAsync(
        AppDbContext db, Guid tournamentId, MatchState state, int take, bool latestFirst, CancellationToken ct)
    {
        var query = db.TournamentMatches
            .Where(m => m.TournamentId == tournamentId && m.State == state)
            .OrderByDescending(m => m.Order);
        var matches = await query.Take(take).ToListAsync(ct);
        if (latestFirst) matches.Reverse();

        var summaries = new List<MatchSummaryDto>();
        foreach (var m in matches)
        {
            var slots = await db.MatchTeams
                .Include(ms => ms.Team)
                .Where(ms => ms.MatchId == m.Id)
                .ToListAsync(ct);
            var results = await db.TournamentTeamMatchResults
                .Where(r => r.MatchId == m.Id)
                .ToDictionaryAsync(r => r.TeamId, ct);
            var teamIds = slots.Select(s => s.TeamId).ToList();
            var players = await db.TeamPlayers
                .Include(tp => tp.Player)
                .ThenInclude(p => p.User)
                .Where(tp => teamIds.Contains(tp.TeamId))
                .ToListAsync(ct);

            var teams = slots
                .Select(s =>
                {
                    var cells = players
                        .Where(tp => tp.TeamId == s.TeamId)
                        .OrderBy(tp => tp.Player.User.Username)
                        .Select(tp => new PlayerCellDto(tp.TournamentPlayerId, tp.Player.UserId, tp.Player.User.Username, tp.Player.User.Name))
                        .ToList();
                    return new TeamSummaryDto(s.TeamId, s.Team.Name, s.Team.Number,
                        results.TryGetValue(s.TeamId, out var r) ? r.GoalsWon : null, cells);
                })
                .ToList();
            summaries.Add(new MatchSummaryDto(m.Id, m.Order, m.State, teams));
        }
        return summaries;
    }
}
