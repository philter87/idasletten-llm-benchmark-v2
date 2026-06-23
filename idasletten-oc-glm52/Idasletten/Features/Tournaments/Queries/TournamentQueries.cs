using Idasletten.Features.Matches;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record ListTournamentsQuery(bool IncludeHistorical = false) : IRequest<List<TournamentView>>;
public record GetTournamentQuery(Guid Id) : IRequest<TournamentView?>;
public record GetTournamentScoreboardQuery(Guid Id) : IRequest<TournamentScoreboard>;
public record GetTournamentMatchesQuery(Guid Id) : IRequest<TournamentMatchesView>;

public record TournamentScoreboard(TournamentView Tournament, List<PlayerView> Players, List<MatchSummaryView> Planned, List<MatchSummaryView> Played);

public record TournamentMatchesView(TournamentView Tournament, List<MatchSummaryView> Planned, List<MatchSummaryView> Played);

public class TournamentQueryHandlers(IdaslettenDbContext db) :
    IRequestHandler<ListTournamentsQuery, List<TournamentView>>,
    IRequestHandler<GetTournamentQuery, TournamentView?>,
    IRequestHandler<GetTournamentScoreboardQuery, TournamentScoreboard>,
    IRequestHandler<GetTournamentMatchesQuery, TournamentMatchesView>
{
    private readonly IdaslettenDbContext _db = db;

    public Task<List<TournamentView>> Handle(ListTournamentsQuery req, CancellationToken ct)
    {
        var q = _db.Tournaments.AsNoTracking();
        if (!req.IncludeHistorical)
            q = q.Where(t => !t.IsArchived && t.ParentTournamentId == null);
        else
            q = q.Where(t => t.ParentTournamentId == null);
        return q.OrderBy(t => t.Name)
                .Select(t => new TournamentView
                {
                    Id = t.Id, Name = t.Name, TeamSize = t.TeamSize, PointsToWin = t.PointsToWin,
                    ScoreSystem = t.ScoreSystem, MaxPlayerCount = t.MaxPlayerCount,
                    IsArchived = t.IsArchived, IsPublic = t.IsPublic,
                    SeedTournamentId = t.SeedTournamentId, ParentTournamentId = t.ParentTournamentId,
                    RoundNumber = t.RoundNumber, PlayerCount = t.Players.Count
                })
                .ToListAsync(ct);
    }

    public async Task<TournamentView?> Handle(GetTournamentQuery req, CancellationToken ct)
    {
        var t = await _db.Tournaments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (t is null) return null;
        return new TournamentView
        {
            Id = t.Id, Name = t.Name, TeamSize = t.TeamSize, PointsToWin = t.PointsToWin,
            ScoreSystem = t.ScoreSystem, MaxPlayerCount = t.MaxPlayerCount,
            IsArchived = t.IsArchived, IsPublic = t.IsPublic,
            SeedTournamentId = t.SeedTournamentId, ParentTournamentId = t.ParentTournamentId,
            RoundNumber = t.RoundNumber, PlayerCount = await _db.TournamentPlayers.CountAsync(p => p.TournamentId == t.Id, ct)
        };
    }

    public async Task<TournamentScoreboard> Handle(GetTournamentScoreboardQuery req, CancellationToken ct)
    {
        var t = await _db.Tournaments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new InvalidOperationException("Tournament not found");
        var view = new TournamentView
        {
            Id = t.Id, Name = t.Name, TeamSize = t.TeamSize, PointsToWin = t.PointsToWin,
            ScoreSystem = t.ScoreSystem, MaxPlayerCount = t.MaxPlayerCount,
            IsArchived = t.IsArchived, IsPublic = t.IsPublic,
            SeedTournamentId = t.SeedTournamentId, ParentTournamentId = t.ParentTournamentId,
            RoundNumber = t.RoundNumber
        };

        var players = await _db.TournamentPlayers.AsNoTracking()
            .Where(p => p.TournamentId == req.Id)
            .OrderByDescending(p => p.Score).ThenByDescending(p => p.PointsWon - p.PointsLost)
            .Select(p => new PlayerView
            {
                Id = p.Id, UserId = p.UserId, Username = p.User!.Username, Name = p.User!.Name,
                ImageUrl = p.User!.ImageUrl, Score = p.Score, WinCount = p.WinCount, LoseCount = p.LoseCount,
                MatchCount = p.MatchCount, Lives = p.Lives, PointsWon = p.PointsWon, PointsLost = p.PointsLost,
                ScoreDiff = p.ScoreDiff
            }).ToListAsync(ct);

        var matches = await _db.TournamentMatches.AsNoTracking()
            .Where(m => m.TournamentId == req.Id)
            .OrderBy(m => m.Order)
            .Select(m => new MatchSummaryView { Id = m.Id, Order = m.Order, State = m.State })
            .ToListAsync(ct);

        var planned = matches.Where(m => m.State == MatchState.Planned).Take(5).ToList();
        var played = matches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.Order).Take(5).ToList();
        foreach (var m in planned) m.Display = $"Match #{m.Order}";
        foreach (var m in played) m.Display = $"Match #{m.Order}";
        return new TournamentScoreboard(view, players, planned, played);
    }

    public async Task<TournamentMatchesView> Handle(GetTournamentMatchesQuery req, CancellationToken ct)
    {
        var t = await _db.Tournaments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new InvalidOperationException("Tournament not found");
        var view = new TournamentView
        {
            Id = t.Id, Name = t.Name, TeamSize = t.TeamSize, PointsToWin = t.PointsToWin,
            ScoreSystem = t.ScoreSystem, MaxPlayerCount = t.MaxPlayerCount,
            IsArchived = t.IsArchived, IsPublic = t.IsPublic,
            SeedTournamentId = t.SeedTournamentId, ParentTournamentId = t.ParentTournamentId,
            RoundNumber = t.RoundNumber
        };
        var matches = await _db.TournamentMatches.AsNoTracking()
            .Where(m => m.TournamentId == req.Id)
            .OrderBy(m => m.Order)
            .Select(m => new MatchSummaryView { Id = m.Id, Order = m.Order, State = m.State })
            .ToListAsync(ct);
        foreach (var m in matches) m.Display = $"Match #{m.Order}";
        return new TournamentMatchesView(view,
            matches.Where(m => m.State == MatchState.Planned).ToList(),
            matches.Where(m => m.State == MatchState.Done).ToList());
    }
}