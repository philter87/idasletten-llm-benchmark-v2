using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record CreatePlannedMatchCommand(Guid TournamentId, IReadOnlyList<string> Team1Initials, IReadOnlyList<string> Team2Initials) : IRequest<Guid>;
public record RecordMatchCommand(Guid TournamentId, Guid? MatchId, IReadOnlyList<string> Team1Initials, IReadOnlyList<string> Team2Initials, int Team1Goals, int Team2Goals) : IRequest<Guid>;
public record PlanSeveralMatchesCommand(Guid TournamentId, int GamesPerPlayer, bool FixedTeams, string SeedingType, Guid? SeedTournamentId = null) : IRequest<int>;
public record CancelMatchCommand(Guid TournamentId, Guid MatchId) : IRequest;

public class CreatePlannedMatchHandler(IdaslettenDbContext db, IMediator mediator, IPublisher publisher) : IRequestHandler<CreatePlannedMatchCommand, Guid>
{
    public async Task<Guid> Handle(CreatePlannedMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await MatchCommandHelpers.BuildMatchAsync(db, mediator, request.TournamentId, request.Team1Initials, request.Team2Initials, MatchState.Planned, null, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new PlannedMatchCreated(request.TournamentId, match.Id), cancellationToken);
        return match.Id;
    }
}

public class RecordMatchHandler(IdaslettenDbContext db, IMediator mediator, IPublisher publisher) : IRequestHandler<RecordMatchCommand, Guid>
{
    public async Task<Guid> Handle(RecordMatchCommand request, CancellationToken cancellationToken)
    {
        TournamentMatch match;
        if (request.MatchId.HasValue)
        {
            match = await db.TournamentMatches.Include(m => m.Teams).ThenInclude(t => t.Players).Include(m => m.Results).SingleAsync(m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, cancellationToken);
            db.TournamentTeamMatchResults.RemoveRange(match.Results);
            db.TournamentTeams.RemoveRange(match.Teams);
            await db.SaveChangesAsync(cancellationToken);
            match.Teams.Clear();
            match.Results.Clear();
            match.State = MatchState.Done;
            match.CompletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            match = await MatchCommandHelpers.BuildMatchAsync(db, mediator, request.TournamentId, request.Team1Initials, request.Team2Initials, MatchState.Done, request.Team1Goals, request.Team2Goals, cancellationToken);
        }

        if (request.MatchId.HasValue)
        {
            await MatchCommandHelpers.AddTeamsAndResultsAsync(db, mediator, match, request.Team1Initials, request.Team2Initials, request.Team1Goals, request.Team2Goals, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await MatchCommandHelpers.RecalculateTournamentAsync(db, request.TournamentId, cancellationToken);
        await publisher.Publish(new MatchRecorded(request.TournamentId, match.Id), cancellationToken);
        return match.Id;
    }
}

public class PlanSeveralMatchesHandler(IdaslettenDbContext db, IMediator mediator) : IRequestHandler<PlanSeveralMatchesCommand, int>
{
    public async Task<int> Handle(PlanSeveralMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.Include(t => t.Players).ThenInclude(p => p.User).SingleAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (request.SeedTournamentId.HasValue && tournament.ParentTournamentId.HasValue) throw new InvalidOperationException("A child tournament cannot set a seed tournament.");
        if (request.SeedTournamentId.HasValue && tournament.SeedTournamentId is null) tournament.SeedTournamentId = request.SeedTournamentId;
        var players = tournament.Players.OrderByDescending(p => p.Score).ThenBy(p => p.User.UserName).ToList();
        if (players.Count < tournament.TeamSize * 2) return 0;

        players = request.SeedingType.ToLowerInvariant() switch
        {
            "equality" => PairBestWorst(players),
            "fair" => PairFair(players),
            _ => players.OrderBy(_ => Guid.NewGuid()).ToList()
        };

        var matchesNeeded = Math.Max(1, (int)Math.Ceiling(players.Count * Math.Max(1, request.GamesPerPlayer) / (double)(tournament.TeamSize * 2)));
        var created = 0;
        for (var i = 0; i < matchesNeeded; i++)
        {
            var rotation = request.FixedTeams ? players : players.Skip(i % players.Count).Concat(players.Take(i % players.Count)).ToList();
            var team1 = rotation.Take(tournament.TeamSize).Select(p => p.User.UserName).ToList();
            var team2 = rotation.Skip(tournament.TeamSize).Take(tournament.TeamSize).Select(p => p.User.UserName).ToList();
            if (team1.Count == tournament.TeamSize && team2.Count == tournament.TeamSize)
            {
                await mediator.Send(new CreatePlannedMatchCommand(request.TournamentId, team1, team2), cancellationToken);
                created++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static List<TournamentPlayer> PairBestWorst(List<TournamentPlayer> players)
    {
        var result = new List<TournamentPlayer>();
        for (var i = 0; i < players.Count / 2; i++) { result.Add(players[i]); result.Add(players[players.Count - 1 - i]); }
        return result;
    }

    private static List<TournamentPlayer> PairFair(List<TournamentPlayer> players)
    {
        var half = (int)Math.Ceiling(players.Count / 2.0);
        return players.Take(half).Zip(players.Skip(half), (top, bottom) => new[] { top, bottom }).SelectMany(pair => pair).ToList();
    }
}

public class CancelMatchHandler(IdaslettenDbContext db) : IRequestHandler<CancelMatchCommand>
{
    public async Task Handle(CancelMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches.SingleAsync(m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, cancellationToken);
        match.State = MatchState.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
    }
}

internal static class MatchCommandHelpers
{
    internal static async Task<TournamentMatch> BuildMatchAsync(IdaslettenDbContext db, IMediator mediator, Guid tournamentId, IReadOnlyList<string> team1Initials, IReadOnlyList<string> team2Initials, MatchState state, int? team1Goals, int? team2Goals, CancellationToken cancellationToken)
{
    var nextOrder = await db.TournamentMatches.Where(m => m.TournamentId == tournamentId).Select(m => (int?)m.Order).MaxAsync(cancellationToken) ?? 0;
    var match = new TournamentMatch { TournamentId = tournamentId, Order = nextOrder + 1, State = state, CompletedAt = state == MatchState.Done ? DateTimeOffset.UtcNow : null };
    db.TournamentMatches.Add(match);
    await db.SaveChangesAsync(cancellationToken);
    await MatchCommandHelpers.AddTeamsAndResultsAsync(db, mediator, match, team1Initials, team2Initials, team1Goals, team2Goals, cancellationToken);
    return match;
}

internal static async Task AddTeamsAndResultsAsync(IdaslettenDbContext db, IMediator mediator, TournamentMatch match, IReadOnlyList<string> team1Initials, IReadOnlyList<string> team2Initials, int? team1Goals, int? team2Goals, CancellationToken cancellationToken)
{
    var team1 = await CreateTeamAsync(db, mediator, match, 1, team1Initials, cancellationToken);
    var team2 = await CreateTeamAsync(db, mediator, match, 2, team2Initials, cancellationToken);
    if (team1Goals.HasValue && team2Goals.HasValue)
    {
        db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult { Match = match, TournamentId = match.TournamentId, Team = team1, GoalsWon = team1Goals.Value, GoalsLost = team2Goals.Value });
        db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult { Match = match, TournamentId = match.TournamentId, Team = team2, GoalsWon = team2Goals.Value, GoalsLost = team1Goals.Value });
    }
}

internal static async Task<TournamentTeam> CreateTeamAsync(IdaslettenDbContext db, IMediator mediator, TournamentMatch match, int number, IReadOnlyList<string> initials, CancellationToken cancellationToken)
{
    var team = new TournamentTeam { TournamentId = match.TournamentId, Match = match, Number = number, Name = $"Team {number}" };
    db.TournamentTeams.Add(team);
    foreach (var initial in initials.Where(value => !string.IsNullOrWhiteSpace(value)))
    {
        var playerId = await mediator.Send(new AddPlayerToTournamentCommand(match.TournamentId, initial), cancellationToken);
        team.Players.Add(new TournamentTeamPlayer { Team = team, TournamentPlayerId = playerId });
    }
    return team;
}

internal static async Task RecalculateTournamentAsync(IdaslettenDbContext db, Guid tournamentId, CancellationToken cancellationToken)
{
    var tournament = await db.Tournaments.Include(t => t.Players).ThenInclude(p => p.User).SingleAsync(t => t.Id == tournamentId, cancellationToken);
    var matches = await db.TournamentMatches.Where(m => m.TournamentId == tournamentId && m.State == MatchState.Done).Include(m => m.Results).Include(m => m.Teams).ThenInclude(t => t.Players).ThenInclude(tp => tp.Player).OrderBy(m => m.Order).ToListAsync(cancellationToken);
    ScoreCalculator.Reset(tournament);
    foreach (var match in matches) ScoreCalculator.Apply(tournament, match);
    await db.SaveChangesAsync(cancellationToken);
}
}
