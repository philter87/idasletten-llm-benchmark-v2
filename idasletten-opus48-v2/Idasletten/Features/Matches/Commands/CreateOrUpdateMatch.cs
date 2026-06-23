using Idasletten.Data;
using Idasletten.Shared;
using Idasletten.Shared.Domain;
using Idasletten.Shared.Events;
using Idasletten.Shared.Graph;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record TeamInput(List<string> Initials, int? Goals);

/// <summary>
/// Creates or updates a match. When every team has a goal value the match is recorded as Done
/// (and standings are recomputed); otherwise it is saved as a Planned match. Unknown initials
/// auto-create users and tournament players.
/// </summary>
public record CreateOrUpdateMatchCommand(Guid TournamentId, Guid? MatchId, List<TeamInput> Teams) : IRequest<Guid>;

public record MatchRecorded(Guid TournamentId, Guid MatchId) : IDomainEvent;
public record MatchPlanned(Guid TournamentId, Guid MatchId) : IDomainEvent;

public class CreateOrUpdateMatchHandler : IRequestHandler<CreateOrUpdateMatchCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IUserImageService _images;
    private readonly ScoreService _scores;
    private readonly IPublisher _publisher;

    public CreateOrUpdateMatchHandler(AppDbContext db, IUserImageService images, ScoreService scores, IPublisher publisher)
    {
        _db = db;
        _images = images;
        _scores = scores;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateOrUpdateMatchCommand cmd, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FirstAsync(t => t.Id == cmd.TournamentId, ct);

        var teamsWithPlayers = cmd.Teams.Where(t => t.Initials.Any(i => !string.IsNullOrWhiteSpace(i))).ToList();
        var done = teamsWithPlayers.Count >= 2 && teamsWithPlayers.All(t => t.Goals.HasValue);

        // Resolve/create players up front.
        var resolved = new List<(TeamInput Input, List<TournamentPlayer> Players)>();
        foreach (var team in teamsWithPlayers)
        {
            var players = new List<TournamentPlayer>();
            foreach (var initials in team.Initials.Where(i => !string.IsNullOrWhiteSpace(i)))
            {
                var user = await Provisioning.GetOrCreateUserAsync(_db, _images, initials, ct: ct);
                players.Add(await Provisioning.AddPlayerAsync(_db, _scores, tournament, user, ct));
            }
            resolved.Add((team, players));
        }
        await _db.SaveChangesAsync(ct);

        TournamentMatch match;
        if (cmd.MatchId is { } id && await _db.TournamentMatches
                .Include(m => m.Results).ThenInclude(r => r.Team)
                .FirstOrDefaultAsync(m => m.Id == id, ct) is { } existing)
        {
            // Replace the old teams/results so editing a match is a clean overwrite.
            var oldTeams = existing.Results.Select(r => r.Team).ToList();
            _db.TournamentTeamMatchResults.RemoveRange(existing.Results);
            _db.TournamentTeams.RemoveRange(oldTeams);
            existing.Results.Clear();
            match = existing;
        }
        else
        {
            int nextOrder = (await _db.TournamentMatches
                .Where(m => m.TournamentId == tournament.Id)
                .MaxAsync(m => (int?)m.Order, ct) ?? 0) + 1;
            match = new TournamentMatch { Id = cmd.MatchId ?? Guid.NewGuid(), TournamentId = tournament.Id, Order = nextOrder };
            _db.TournamentMatches.Add(match);
        }

        match.State = done ? MatchState.Done : MatchState.Planned;

        int nextNumber = (await _db.TournamentTeams
            .Where(t => t.TournamentId == tournament.Id)
            .MaxAsync(t => (int?)t.Number, ct) ?? 0) + 1;

        var goals = resolved.Select(r => r.Input.Goals ?? 0).ToList();
        for (int i = 0; i < resolved.Count; i++)
        {
            var team = new TournamentTeam
            {
                TournamentId = tournament.Id,
                Number = nextNumber++,
            };
            team.Name = $"Team {team.Number}";
            team.Players.AddRange(resolved[i].Players);
            _db.TournamentTeams.Add(team);

            int goalsWon = goals[i];
            int goalsLost = goals.Where((_, j) => j != i).DefaultIfEmpty(0).Max();
            _db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TeamId = team.Id,
                GoalsWon = goalsWon,
                GoalsLost = goalsLost
            });
        }

        await _db.SaveChangesAsync(ct);

        // Recompute standings whenever a completed result is involved (including edits).
        if (done || cmd.MatchId is not null)
            await _scores.RecalculateAsync(tournament.Id, ct);

        if (done)
            await _publisher.Publish(new MatchRecorded(tournament.Id, match.Id), ct);
        else
            await _publisher.Publish(new MatchPlanned(tournament.Id, match.Id), ct);

        return match.Id;
    }
}
