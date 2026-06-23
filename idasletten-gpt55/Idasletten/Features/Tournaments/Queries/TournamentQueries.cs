using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record TournamentCard(Guid Id, string Name, ScoreSystem ScoreSystem, int PlayerCount, int DoneMatchCount, bool IsPublic, bool IsArchived, int? RoundNumber);
public record PlayerRow(Guid UserId, Guid TournamentPlayerId, string Initials, string Name, double Score, double ScoreDiff, int Wins, int Losses, int Matches, int Lives, int PointsWon, int PointsLost);
public record MatchRow(Guid Id, int Order, string Summary, MatchState State, string Score, DateTimeOffset? CompletedAt);
public record TournamentDetail(Guid Id, string Name, int TeamSize, int PointsToWin, ScoreSystem ScoreSystem, bool IsArchived, bool IsPublic, IReadOnlyList<PlayerRow> Players, IReadOnlyList<MatchRow> PlannedMatches, IReadOnlyList<MatchRow> RecentMatches);

public record ListTournamentsQuery(bool Historical = false, bool IncludeChildren = false) : IRequest<IReadOnlyList<TournamentCard>>;
public record GetTournamentDetailQuery(Guid TournamentId) : IRequest<TournamentDetail?>;

public class ListTournamentsHandler(IdaslettenDbContext db) : IRequestHandler<ListTournamentsQuery, IReadOnlyList<TournamentCard>>
{
    public async Task<IReadOnlyList<TournamentCard>> Handle(ListTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tournaments.AsNoTracking().Include(t => t.Players).Include(t => t.Matches).AsQueryable();
        if (!request.Historical) query = query.Where(t => t.IsPublic && !t.IsArchived);
        if (!request.IncludeChildren) query = query.Where(t => t.ParentTournamentId == null);
        var tournaments = await query.ToListAsync(cancellationToken);
        return tournaments.OrderBy(t => t.IsArchived).ThenByDescending(t => t.CreatedAt).Select(t => new TournamentCard(t.Id, t.Name, t.ScoreSystem, t.Players.Count, t.Matches.Count(m => m.State == MatchState.Done), t.IsPublic, t.IsArchived, t.RoundNumber)).ToList();
    }
}

public class GetTournamentDetailHandler(IdaslettenDbContext db) : IRequestHandler<GetTournamentDetailQuery, TournamentDetail?>
{
    public async Task<TournamentDetail?> Handle(GetTournamentDetailQuery request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.AsNoTracking().Include(t => t.Players).ThenInclude(p => p.User).Include(t => t.Matches).ThenInclude(m => m.Results).Include(t => t.Matches).ThenInclude(m => m.Teams).ThenInclude(team => team.Players).ThenInclude(tp => tp.Player).ThenInclude(p => p.User).SingleOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (tournament is null) return null;
        var players = tournament.Players.OrderByDescending(p => p.Score).ThenByDescending(p => p.WinCount).ThenByDescending(p => p.PointsWon - p.PointsLost).Select(p => new PlayerRow(p.UserId, p.Id, p.User.UserName, p.User.Name, p.Score, p.ScoreDiff, p.WinCount, p.LoseCount, p.MatchCount, p.Lives, p.PointsWon, p.PointsLost)).ToList();
        var planned = tournament.Matches.Where(m => m.State == MatchState.Planned).OrderBy(m => m.Order).Take(5).Select(ToRow).ToList();
        var recent = tournament.Matches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.CompletedAt).ThenByDescending(m => m.Order).Take(5).Select(ToRow).ToList();
        return new TournamentDetail(tournament.Id, tournament.Name, tournament.TeamSize, tournament.PointsToWin, tournament.ScoreSystem, tournament.IsArchived, tournament.IsPublic, players, planned, recent);
    }

    private static MatchRow ToRow(TournamentMatch match)
    {
        var teams = match.Teams.OrderBy(t => t.Number).Select(t => string.Join("+", t.Players.Select(tp => tp.Player.User.UserName))).ToList();
        var teamNumbers = match.Teams.ToDictionary(t => t.Id, t => t.Number);
        var score = match.Results.Count == 0 ? "-" : string.Join(" - ", match.Results.OrderBy(r => teamNumbers.GetValueOrDefault(r.TeamId)).Select(r => r.GoalsWon));
        return new MatchRow(match.Id, match.Order, string.Join(" vs ", teams), match.State, score, match.CompletedAt);
    }
}
