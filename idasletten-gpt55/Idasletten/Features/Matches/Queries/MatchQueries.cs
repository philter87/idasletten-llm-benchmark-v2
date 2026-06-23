using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record MatchEditor(Guid? MatchId, Guid TournamentId, string TournamentName, int PointsToWin, string Team1Initials, string Team2Initials, int Team1Goals, int Team2Goals, bool IsDone);
public record MatchList(Guid TournamentId, string TournamentName, IReadOnlyList<MatchRow> Planned, IReadOnlyList<MatchRow> Done);

public record GetMatchEditorQuery(Guid TournamentId, Guid? MatchId) : IRequest<MatchEditor?>;
public record GetMatchListQuery(Guid TournamentId) : IRequest<MatchList?>;

public class GetMatchEditorHandler(IdaslettenDbContext db) : IRequestHandler<GetMatchEditorQuery, MatchEditor?>
{
    public async Task<MatchEditor?> Handle(GetMatchEditorQuery request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.AsNoTracking().SingleOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (tournament is null) return null;
        if (!request.MatchId.HasValue) return new MatchEditor(null, tournament.Id, tournament.Name, tournament.PointsToWin, "", "", tournament.PointsToWin, 0, false);
        var match = await db.TournamentMatches.AsNoTracking().Include(m => m.Teams).ThenInclude(t => t.Players).ThenInclude(tp => tp.Player).ThenInclude(p => p.User).Include(m => m.Results).SingleOrDefaultAsync(m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, cancellationToken);
        if (match is null) return null;
        var teams = match.Teams.OrderBy(t => t.Number).ToList();
        var results = match.Results.OrderBy(r => r.Team.Number).ToList();
        return new MatchEditor(match.Id, tournament.Id, tournament.Name, tournament.PointsToWin, Initials(teams.ElementAtOrDefault(0)), Initials(teams.ElementAtOrDefault(1)), results.ElementAtOrDefault(0)?.GoalsWon ?? tournament.PointsToWin, results.ElementAtOrDefault(1)?.GoalsWon ?? 0, match.State == MatchState.Done);
    }

    private static string Initials(TournamentTeam? team) => team is null ? "" : string.Join(" ", team.Players.Select(tp => tp.Player.User.UserName));
}

public class GetMatchListHandler(IdaslettenDbContext db) : IRequestHandler<GetMatchListQuery, MatchList?>
{
    public async Task<MatchList?> Handle(GetMatchListQuery request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.AsNoTracking().Include(t => t.Matches).ThenInclude(m => m.Results).Include(t => t.Matches).ThenInclude(m => m.Teams).ThenInclude(team => team.Players).ThenInclude(tp => tp.Player).ThenInclude(p => p.User).SingleOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (tournament is null) return null;
        var rows = tournament.Matches.Where(m => m.State != MatchState.Cancelled).Select(ToRow).ToList();
        return new MatchList(tournament.Id, tournament.Name, rows.Where(r => r.State == MatchState.Planned).OrderBy(r => r.Order).ToList(), rows.Where(r => r.State == MatchState.Done).OrderByDescending(r => r.Order).ToList());
    }

    private static MatchRow ToRow(TournamentMatch match)
    {
        var teams = match.Teams.OrderBy(t => t.Number).Select(t => string.Join("+", t.Players.Select(tp => tp.Player.User.UserName))).ToList();
        var teamNumbers = match.Teams.ToDictionary(t => t.Id, t => t.Number);
        var score = match.Results.Count == 0 ? "-" : string.Join(" - ", match.Results.OrderBy(r => teamNumbers.GetValueOrDefault(r.TeamId)).Select(r => r.GoalsWon));
        return new MatchRow(match.Id, match.Order, string.Join(" vs ", teams), match.State, score, match.CompletedAt);
    }
}
