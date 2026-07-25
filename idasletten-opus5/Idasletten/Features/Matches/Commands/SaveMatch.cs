using Idasletten.Features.Matches.Events;
using Idasletten.Features.Scoring;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record MatchTeamInput(IReadOnlyList<string> Initials, int Goals);

/// <summary>
/// Creates, plans, records and edits a match - the create-match page uses this one command for all of it.
/// The page generates <see cref="MatchId"/> up front, so the same id can be used to edit the match later.
/// Unknown initials create the user and add them to the tournament.
/// </summary>
public record SaveMatch(
    Guid TournamentId,
    Guid MatchId,
    IReadOnlyList<MatchTeamInput> Teams,
    bool AsPlanned = false) : IRequest<Guid>;

public class SaveMatchHandler(AppDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<SaveMatch, Guid>
{
    public async Task<Guid> Handle(SaveMatch request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new ArgumentException("Unknown tournament.", nameof(request));

        var teamInputs = request.Teams
            .Where(team => team.Initials.Any(initials => !string.IsNullOrWhiteSpace(initials)))
            .ToList();

        if (teamInputs.Count < 2)
        {
            throw new InvalidOperationException("A match needs at least two teams with players.");
        }

        if (!request.AsPlanned && teamInputs.All(team => team.Goals <= 0))
        {
            throw new InvalidOperationException("At least one team has to score before a match is done.");
        }

        var teams = new List<(TournamentTeam Team, int Goals)>();
        foreach (var input in teamInputs)
        {
            var playerIds = await MatchTeams.ResolvePlayerIdsAsync(
                db, sender, tournament, input.Initials, cancellationToken);

            if (playerIds.Count == 0)
            {
                throw new InvalidOperationException("A team needs at least one player.");
            }

            if (teams.Any(existing => SamePlayers(existing.Team, playerIds)))
            {
                throw new InvalidOperationException("A player cannot play against themselves.");
            }

            var team = await MatchTeams.GetOrCreateTeamAsync(db, tournament, playerIds, cancellationToken);
            teams.Add((team, Math.Max(0, input.Goals)));
        }

        var match = await db.TournamentMatches
            .FirstOrDefaultAsync(
                m => m.Id == request.MatchId && m.TournamentId == tournament.Id, cancellationToken);

        var wasAlreadyPlayed = match?.State == MatchState.Done;

        if (match is null)
        {
            var nextOrder = await db.TournamentMatches
                .Where(m => m.TournamentId == tournament.Id)
                .Select(m => (int?)m.Order)
                .MaxAsync(cancellationToken) ?? 0;

            match = new TournamentMatch
            {
                Id = request.MatchId == Guid.Empty ? Guid.NewGuid() : request.MatchId,
                TournamentId = tournament.Id,
                Order = nextOrder + 1,
            };

            db.TournamentMatches.Add(match);
        }
        else
        {
            // The lines of the match are written from scratch. They are deleted through the DbSet and
            // not through the navigation collection, so EF does not also try to orphan-delete them.
            var previousResults = await db.TournamentTeamMatchResults
                .Where(result => result.MatchId == match.Id)
                .ToListAsync(cancellationToken);

            db.TournamentTeamMatchResults.RemoveRange(previousResults);
        }

        match.State = request.AsPlanned ? MatchState.Planned : MatchState.Done;
        match.PlayedUtc = request.AsPlanned ? null : match.PlayedUtc ?? DateTime.UtcNow;

        var totalGoals = teams.Sum(team => team.Goals);
        foreach (var (team, goals) in teams)
        {
            db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TeamId = team.Id,
                GoalsWon = request.AsPlanned ? 0 : goals,
                // With the usual two teams this is simply the goals of the opponent.
                GoalsLost = request.AsPlanned ? 0 : totalGoals - goals,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        await TournamentScoring.RecalculateAsync(db, tournament.Id, cancellationToken);

        if (request.AsPlanned)
        {
            await publisher.Publish(new MatchPlanned(tournament.Id, match.Id, match.Order), cancellationToken);
        }
        else
        {
            await publisher.Publish(
                new MatchResultSaved(tournament.Id, match.Id, match.Order, wasAlreadyPlayed),
                cancellationToken);
        }

        return match.Id;
    }

    private static bool SamePlayers(TournamentTeam team, IReadOnlyList<Guid> playerIds) =>
        team.Players.Count == playerIds.Count &&
        team.Players.All(p => playerIds.Contains(p.TournamentPlayerId));
}
