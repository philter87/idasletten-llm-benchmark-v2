using Idasletten.Features.Scoring;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record TeamResultInput(List<string> Initials, int Goals);

/// <summary>
/// Records a match result. When MatchId points to a planned match it is completed;
/// when it points to a Done match the result is replaced and the whole tournament
/// is recalculated (the caller must ensure the user is logged in for that case).
/// </summary>
public record RecordMatchResultCommand(
    Guid TournamentId,
    List<TeamResultInput> Teams,
    Guid? MatchId = null) : IRequest<TournamentMatch>;

public record MatchResultRecorded(Guid TournamentId, Guid MatchId) : INotification;
public record MatchResultUpdated(Guid TournamentId, Guid MatchId) : INotification;

public class RecordMatchResultHandler(AppDbContext db, IMediator mediator, IPublisher publisher)
    : IRequestHandler<RecordMatchResultCommand, TournamentMatch>
{
    public async Task<TournamentMatch> Handle(RecordMatchResultCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        var teams = await TeamResolver.ResolveTeams(
            db, mediator, tournament,
            request.Teams.Select(t => (IReadOnlyList<string>)t.Initials).ToList(), ct);

        TournamentMatch? match = null;
        if (request.MatchId is Guid matchId)
            match = await db.TournamentMatches
                .Include(m => m.Results)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournament.Id, ct);

        var isEditOfDoneMatch = match?.State == MatchState.Done;

        if (match is null)
        {
            match = new TournamentMatch
            {
                Id = request.MatchId ?? Guid.NewGuid(),
                TournamentId = tournament.Id,
                Order = await NextOrder(tournament.Id, ct)
            };
            db.TournamentMatches.Add(match);
        }
        else
        {
            db.TournamentTeamMatchResults.RemoveRange(match.Results);
            match.Results.Clear();
        }

        match.State = MatchState.Done;
        match.PlayedAt ??= DateTime.UtcNow;

        for (var i = 0; i < teams.Count; i++)
        {
            var goalsLost = request.Teams.Where((_, j) => j != i).Sum(t => t.Goals);
            var result = new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TeamId = teams[i].Id,
                Team = teams[i],
                GoalsWon = request.Teams[i].Goals,
                GoalsLost = goalsLost
            };
            // Into the collection first, then an explicit Add: results reachable from
            // an already-tracked match would otherwise be attached as existing
            // (their Guid keys are pre-set), and Add's fixup skips items already
            // present in the collection.
            match.Results.Add(result);
            db.TournamentTeamMatchResults.Add(result);
        }

        if (isEditOfDoneMatch)
        {
            await db.SaveChangesAsync(ct);
            await mediator.Send(new RecalculateTournamentCommand(tournament.Id), ct);
            await publisher.Publish(new MatchResultUpdated(tournament.Id, match.Id), ct);
        }
        else
        {
            var playersByTeamId = teams.ToDictionary(t => t.Id, t => t.Players);
            ScoringEngine.ApplyMatch(tournament.ScoreSystem, match.Results, playersByTeamId);
            await db.SaveChangesAsync(ct);
            await publisher.Publish(new MatchResultRecorded(tournament.Id, match.Id), ct);
        }

        return match;
    }

    private async Task<int> NextOrder(Guid tournamentId, CancellationToken ct)
    {
        var maxOrder = await db.TournamentMatches
            .Where(m => m.TournamentId == tournamentId)
            .Select(m => (int?)m.Order)
            .MaxAsync(ct);
        return (maxOrder ?? 0) + 1;
    }
}
