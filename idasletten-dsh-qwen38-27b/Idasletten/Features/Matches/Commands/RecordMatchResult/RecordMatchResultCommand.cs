using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Matches.Events;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users.Commands.FindOrCreateUser;
using Idasletten.Models;
using Idasletten.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.RecordMatchResult;

public sealed class TeamInput
{
    public List<string> PlayerInitials { get; set; } = new();
    public int Goals { get; set; }
}

/// <summary>
/// Records (or edits) a match result. Without <see cref="MatchId"/> a new
/// finished match is created. With a Planned match id the planned match is
/// completed. With a Done match id the result is replaced and all scores are
/// recalculated — requires authentication.
/// </summary>
public sealed record RecordMatchResultCommand(Guid TournamentId, Guid? MatchId, IReadOnlyList<TeamInput> Teams) : IRequest<Guid>;

public sealed class RecordMatchResultCommandHandler : IRequestHandler<RecordMatchResultCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly ScoringEngine _scoring;
    private readonly IMediator _mediator;
    private readonly IPublisher _publisher;
    private readonly IHttpContextAccessor _http;

    public RecordMatchResultCommandHandler(AppDbContext db, ScoringEngine scoring, IMediator mediator, IPublisher publisher, IHttpContextAccessor http)
    {
        _db = db;
        _scoring = scoring;
        _mediator = mediator;
        _publisher = publisher;
        _http = http;
    }

    private bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public async Task<Guid> Handle(RecordMatchResultCommand request, CancellationToken cancellationToken)
    {
        if (request.Teams is null || request.Teams.Count < 2 || request.Teams.Count > 4)
            throw new FeatureException("A match needs between 2 and 4 teams.");
        foreach (var t in request.Teams)
        {
            if (t.Goals < 0) throw new FeatureException("Goals cannot be negative.");
            if (t.PlayerInitials.Count == 0) throw new FeatureException("Every team needs players.");
        }

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new FeatureException("Tournament not found.");
        if (tournament.IsArchived)
            throw new FeatureException("This tournament is archived; results can no longer be recorded.");

        // No player may sit on two teams.
        var allInitials = request.Teams.SelectMany(t => t.PlayerInitials).Select(i => FindOrCreateUserCommandHandler.Normalize(i)).ToList();
        if (allInitials.Distinct().Count() != allInitials.Count)
            throw new FeatureException("A player cannot play for two teams in the same match.");

        TournamentMatch? match = null;
        if (request.MatchId is Guid mid)
        {
            match = await _db.TournamentMatches
                .FirstOrDefaultAsync(m => m.Id == mid && m.TournamentId == tournament.Id, cancellationToken)
                ?? throw new FeatureException("Match not found in this tournament.");
            if (match.State == MatchState.Cancelled)
                throw new FeatureException("This match was cancelled.");
            if (match.State == MatchState.Done && !IsAuthenticated)
                throw new FeatureException("You must be logged in to edit a completed match.");
        }

        // Resolve players for all teams (auto-creating users/players as needed).
        var resolved = new List<List<Guid>>();
        foreach (var t in request.Teams)
            resolved.Add(await MatchSupport.ResolveInitialsAsync(_db, _scoring, _mediator, _publisher, tournament, t.PlayerInitials, true, cancellationToken));

        // Rebuild teams/links/results for this match.
        if (match is not null)
            await ResetMatchAsync(match, cancellationToken);
        else
        {
            match = new TournamentMatch
            {
                TournamentId = tournament.Id,
                Order = await MatchSupport.NextOrderAsync(_db, tournament.Id, cancellationToken),
                State = MatchState.Planned
            };
            _db.TournamentMatches.Add(match);
            await _db.SaveChangesAsync(cancellationToken);
        }

        for (var i = 0; i < request.Teams.Count; i++)
        {
            var team = await MatchSupport.CreateTeamAsync(_db, tournament, i + 1, resolved[i], cancellationToken);
            _db.MatchTeams.Add(new MatchTeam { MatchId = match.Id, TeamId = team.Id });
            var opponents = request.Teams.Where((_, j) => j != i).Sum(t => t.Goals);
            _db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TeamId = team.Id,
                GoalsWon = request.Teams[i].Goals,
                GoalsLost = opponents
            });
        }
        match.State = MatchState.Done;
        await _db.SaveChangesAsync(cancellationToken);

        await _scoring.RecalculateTournamentAsync(_db, tournament, cancellationToken);
        await _publisher.Publish(new MatchResultRecorded(match.Id, tournament.Id, request.MatchId is null), cancellationToken);
        return match.Id;
    }

    /// <summary>Detach everything from the match (teams, links, results) so a new configuration can be written.</summary>
    private async Task ResetMatchAsync(TournamentMatch match, CancellationToken ct)
    {
        var teamIds = await _db.MatchTeams.Where(mt => mt.MatchId == match.Id).Select(mt => mt.TeamId).ToListAsync(ct);
        var playersOfTeams = await _db.TeamPlayers.Where(tp => teamIds.Contains(tp.TeamId)).ToListAsync(ct);
        _db.TeamPlayers.RemoveRange(playersOfTeams);
        _db.MatchTeams.RemoveRange(await _db.MatchTeams.Where(mt => mt.MatchId == match.Id).ToListAsync(ct));
        _db.TournamentTeamMatchResults.RemoveRange(await _db.TournamentTeamMatchResults.Where(r => r.MatchId == match.Id).ToListAsync(ct));
        _db.TournamentTeams.RemoveRange(await _db.TournamentTeams.Where(t => teamIds.Contains(t.Id)).ToListAsync(ct));
        await _db.SaveChangesAsync(ct);
    }
}
