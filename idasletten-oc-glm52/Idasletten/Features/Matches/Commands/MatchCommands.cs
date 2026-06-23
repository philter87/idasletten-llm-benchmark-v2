using Idasletten.Features.Players;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record CreateMatchCommand(
    Guid TournamentId,
    Guid? MatchId,
    List<List<string>> TeamsInitials,
    List<int> Scores
) : IRequest<Guid>;

public record PlanMatchCommand(Guid TournamentId, List<List<string>> TeamsInitials) : IRequest<Guid>;

public enum SeedingType { Random, Equality, Fair }

public record PlanSeveralMatchesCommand(
    Guid TournamentId, int GamesPerPlayer, bool FixedTeam, SeedingType SeedingType = SeedingType.Random
) : IRequest<int>;

public class MatchCommandHandlers(
    IdaslettenDbContext db,
    IMediator mediator,
    Microsoft.AspNetCore.Http.IHttpContextAccessor httpContext
) : IRequestHandler<CreateMatchCommand, Guid>,
        IRequestHandler<PlanMatchCommand, Guid>,
        IRequestHandler<PlanSeveralMatchesCommand, int>
{
    private readonly IdaslettenDbContext _db = db;
    private readonly IMediator _mediator = mediator;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _http = httpContext;

    public async Task<Guid> Handle(CreateMatchCommand req, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync(req.TournamentId, ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var matchId = req.MatchId ?? Guid.NewGuid();
        var match = req.MatchId.HasValue
            ? await _db.TournamentMatches.FirstOrDefaultAsync(m => m.Id == req.MatchId, ct)
            : null;

        if (match?.State == MatchState.Done)
        {
            var isAuth = _http.HttpContext?.User?.Identity?.IsAuthenticated == true;
            if (!isAuth) throw new UnauthorizedAccessException("Login required to edit a done match.");
        }

        match ??= new TournamentMatch { Id = matchId, TournamentId = req.TournamentId, State = MatchState.Planned };
        match.Id = matchId;

        var maxOrder = await _db.TournamentMatches
            .Where(m => m.TournamentId == req.TournamentId)
            .MaxAsync(m => (int?)m.Order, ct) ?? 0;
        match.Order = maxOrder + 1;
        if (match.Teams is not null) match.Teams.Clear();
        else match.Teams = new List<TournamentTeam>();

        if (match.Id == Guid.Empty) match.Id = matchId;

        var teamNumber = 1;
        var teams = new List<TournamentTeam>();
        for (int i = 0; i < req.TeamsInitials.Count; i++)
        {
            var team = new TournamentTeam { TournamentId = req.TournamentId, Number = teamNumber, Name = $"Team {teamNumber++}" };
            foreach (var initials in req.TeamsInitials[i])
            {
                var userId = await _mediator.Send(new EnsureUserCommand(initials), ct);
                var tp = await _db.TournamentPlayers.FirstOrDefaultAsync(
                    p => p.TournamentId == req.TournamentId && p.UserId == userId, ct);
                if (tp is null)
                {
                    var scoring = new ScoringSystemSelector().For(tournament);
                    tp = new TournamentPlayer { UserId = userId, TournamentId = req.TournamentId };
                    scoring.Initialise(tp);
                    _db.TournamentPlayers.Add(tp);
                    await _mediator.Publish(new PlayerAdded(req.TournamentId, userId), ct);
                }
                team.Players.Add(tp);
            }
            teams.Add(team);
        }
        _db.TournamentTeams.AddRange(teams);
        match.Teams = teams;
        if (!await _db.TournamentMatches.AnyAsync(m => m.Id == match.Id, ct))
            _db.TournamentMatches.Add(match);
        await _db.SaveChangesAsync(ct);

        var results = new List<TournamentTeamMatchResult>();
        for (int i = 0; i < teams.Count; i++)
        {
            var others = req.Scores.Where((_, idx) => idx != i).Sum();
            results.Add(new TournamentTeamMatchResult
            {
                TeamId = teams[i].Id,
                GoalsWon = i < req.Scores.Count ? req.Scores[i] : 0,
                GoalsLost = others
            });
        }
        var recorder = new MatchRecorder(_db);
        await recorder.RecordAsync(match, results);
        await _mediator.Publish(new MatchRecorded(req.TournamentId, match.Id), ct);
        return match.Id;
    }

    public async Task<Guid> Handle(PlanMatchCommand req, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync(req.TournamentId, ct)
            ?? throw new InvalidOperationException("Tournament not found");
        var order = await _db.TournamentMatches
            .Where(m => m.TournamentId == req.TournamentId)
            .MaxAsync(m => (int?)m.Order, ct) ?? 0;
        var match = new TournamentMatch { TournamentId = req.TournamentId, Order = order + 1, State = MatchState.Planned };
        var teamNumber = 1;
        foreach (var teamInitials in req.TeamsInitials)
        {
            var team = new TournamentTeam { TournamentId = req.TournamentId, Number = teamNumber, Name = $"Team {teamNumber++}" };
            foreach (var initials in teamInitials)
            {
                var userId = await _mediator.Send(new EnsureUserCommand(initials), ct);
                var tp = await _db.TournamentPlayers.FirstOrDefaultAsync(
                    p => p.TournamentId == req.TournamentId && p.UserId == userId, ct);
                if (tp is null)
                {
                    tp = new TournamentPlayer { UserId = userId, TournamentId = req.TournamentId };
                    new ScoringSystemSelector().For(tournament).Initialise(tp);
                    _db.TournamentPlayers.Add(tp);
                }
                team.Players.Add(tp);
            }
            match.Teams.Add(team);
        }
        _db.TournamentMatches.Add(match);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new MatchPlanned(req.TournamentId, match.Id), ct);
        return match.Id;
    }

    public async Task<int> Handle(PlanSeveralMatchesCommand req, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync(req.TournamentId, ct)
            ?? throw new InvalidOperationException("Tournament not found");

        List<Guid> ranked;
        if (tournament.SeedTournamentId is Guid seedId)
        {
            ranked = await _db.TournamentPlayers.AsNoTracking()
                .Where(p => p.TournamentId == seedId)
                .OrderByDescending(p => p.Score)
                .Select(p => p.UserId).ToListAsync(ct);
        }
        else
        {
            ranked = await _db.TournamentPlayers.AsNoTracking()
                .Where(p => p.TournamentId == req.TournamentId)
                .OrderByDescending(p => p.Score)
                .Select(p => p.UserId).ToListAsync(ct);
        }
        if (ranked.Count < tournament.TeamSize * 2) return 0;

        foreach (var uid in ranked)
        {
            var exists = await _db.TournamentPlayers.AnyAsync(
                p => p.TournamentId == req.TournamentId && p.UserId == uid, ct);
            if (!exists)
            {
                var tp = new TournamentPlayer { UserId = uid, TournamentId = req.TournamentId };
                new ScoringSystemSelector().For(tournament).Initialise(tp);
                _db.TournamentPlayers.Add(tp);
                await _mediator.Publish(new PlayerAdded(req.TournamentId, uid), ct);
            }
        }
        await _db.SaveChangesAsync(ct);

        var totalMatches = (int)Math.Ceiling(req.GamesPerPlayer * (double)ranked.Count / (tournament.TeamSize * 2));
        var rnd = new Random(7);
        int created = 0;
        var baseOrder = await _db.TournamentMatches
            .Where(m => m.TournamentId == req.TournamentId)
            .MaxAsync(m => (int?)m.Order, ct) ?? 0;
        for (int m = 0; m < totalMatches; m++)
        {
            var (a, b) = PickTeams(ranked, req.SeedingType, rnd, tournament.TeamSize, req.FixedTeam, m);
            var match = new TournamentMatch { TournamentId = req.TournamentId, Order = baseOrder + m + 1, State = MatchState.Planned };
            match.Teams = new List<TournamentTeam>
            {
                await BuildTeamAsync(_db, req.TournamentId, 1, a, ct),
                await BuildTeamAsync(_db, req.TournamentId, 2, b, ct)
            };
            _db.TournamentMatches.Add(match);
            created++;
        }
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new MatchesPlanned(req.TournamentId, created), ct);
        return created;
    }

    private static (List<Guid> a, List<Guid> b) PickTeams(List<Guid> ranked, SeedingType type, Random rnd, int teamSize, bool fixedTeam, int matchIdx)
    {
        if (type == SeedingType.Random)
        {
            var shuffled = ranked.OrderBy(_ => rnd.Next()).ToList();
            return (shuffled.Take(teamSize).ToList(), shuffled.Skip(teamSize).Take(teamSize).ToList());
        }
        var half = ranked.Count / 2;
        var topHalf = ranked.Take(half).ToList();
        var botHalf = ranked.Skip(half).ToList();
        if (type == SeedingType.Equality)
        {
            var top = topHalf.Take(teamSize).ToList();
            var b = topHalf.Skip(teamSize).Take(teamSize).Concat(botHalf.Take(teamSize)).Take(teamSize).ToList();
            if (!fixedTeam)
            {
                var off = matchIdx % Math.Max(1, top.Count);
                top = top.Skip(off).Concat(top.Take(off)).ToList();
            }
            return (top, b);
        }
        // Fair: 1+N, 2+(N−1) pairing into two teams alternating
        var a2 = new List<Guid>();
        var b2 = new List<Guid>();
        for (int i = 0; i < half; i++)
        {
            a2.Add(topHalf[i]);
            a2.Add(botHalf[i]);
        }
        for (int i = 0; i < teamSize; i++) b2.Add(a2[a2.Count - 1 - i]);
        a2 = a2.Take(teamSize).ToList();
        b2 = b2.Take(teamSize).ToList();
        if (!fixedTeam && half > 0)
        {
            var off = matchIdx % half;
            a2 = topHalf.Skip(off).Take(teamSize).ToList();
            if (a2.Count < teamSize) a2 = topHalf.Take(teamSize).ToList();
            b2 = botHalf.Skip(off).Take(teamSize).ToList();
            if (b2.Count < teamSize) b2 = botHalf.Take(teamSize).ToList();
        }
        return (a2, b2);
    }

    private static async Task<TournamentTeam> BuildTeamAsync(IdaslettenDbContext db, Guid tournamentId, int number, List<Guid> userIds, CancellationToken ct)
    {
        var team = new TournamentTeam { TournamentId = tournamentId, Number = number, Name = $"Team {number}" };
        foreach (var uid in userIds)
        {
            var tp = await db.TournamentPlayers.FirstAsync(p => p.TournamentId == tournamentId && p.UserId == uid, ct);
            team.Players.Add(tp);
        }
        return team;
    }
}

public record MatchRecorded(Guid TournamentId, Guid MatchId) : INotification;
public record MatchPlanned(Guid TournamentId, Guid MatchId) : INotification;
public record MatchesPlanned(Guid TournamentId, int Count) : INotification;