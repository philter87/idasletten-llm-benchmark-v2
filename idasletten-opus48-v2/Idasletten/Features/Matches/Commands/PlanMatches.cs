using Idasletten.Data;
using Idasletten.Shared.Domain;
using Idasletten.Shared.Events;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

/// <summary>
/// Plans several matches at once for a tournament, optionally seeding team composition from a
/// previous tournament's standings.
/// </summary>
public record PlanMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeam,
    SeedingType Seeding,
    Guid? SeedTournamentId = null) : IRequest<int>;

public record MatchesPlanned(Guid TournamentId, int Count) : IDomainEvent;

public class PlanMatchesHandler : IRequestHandler<PlanMatchesCommand, int>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;
    public PlanMatchesHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<int> Handle(PlanMatchesCommand cmd, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FirstAsync(t => t.Id == cmd.TournamentId, ct);

        // Persist a newly chosen seed tournament (only allowed when this tournament has no parent).
        if (cmd.SeedTournamentId is { } seedId && tournament.SeedTournamentId is null && tournament.ParentTournamentId is null)
        {
            tournament.SeedTournamentId = seedId;
        }
        var effectiveSeedId = tournament.SeedTournamentId;

        var players = await _db.TournamentPlayers
            .Where(p => p.TournamentId == tournament.Id)
            .Select(p => p.UserId)
            .ToListAsync(ct);

        // Seed scores from the source tournament's standings (0 when absent → unranked).
        Dictionary<Guid, double> seedScores = new();
        if (effectiveSeedId is { } sid && cmd.Seeding != SeedingType.Random)
        {
            seedScores = await _db.TournamentPlayers
                .Where(p => p.TournamentId == sid)
                .ToDictionaryAsync(p => p.UserId, p => p.Score, ct);
        }

        var input = players
            .Select(id => (UserId: id, SeedScore: seedScores.GetValueOrDefault(id)))
            .ToList();

        var planned = SeedingPlanner.Plan(
            input, tournament.TeamSize, cmd.GamesPerPlayer, cmd.FixedTeam, cmd.Seeding, Random.Shared);
        if (planned.Count == 0) return 0;

        int nextOrder = (await _db.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .MaxAsync(m => (int?)m.Order, ct) ?? 0) + 1;
        int nextNumber = (await _db.TournamentTeams
            .Where(t => t.TournamentId == tournament.Id)
            .MaxAsync(t => (int?)t.Number, ct) ?? 0) + 1;

        // Map already-loaded tournament players so EF tracks the same instances.
        var trackedPlayers = await _db.TournamentPlayers
            .Where(p => p.TournamentId == tournament.Id)
            .ToDictionaryAsync(p => p.UserId, ct);

        foreach (var matchTeams in planned)
        {
            var match = new TournamentMatch
            {
                TournamentId = tournament.Id,
                Order = nextOrder++,
                State = MatchState.Planned
            };
            _db.TournamentMatches.Add(match);

            foreach (var teamUserIds in matchTeams)
            {
                var team = new TournamentTeam
                {
                    TournamentId = tournament.Id,
                    Number = nextNumber,
                    Name = $"Team {nextNumber}"
                };
                nextNumber++;
                team.Players.AddRange(teamUserIds.Select(id => trackedPlayers[id]));
                _db.TournamentTeams.Add(team);

                _db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
                {
                    MatchId = match.Id,
                    TournamentId = tournament.Id,
                    TeamId = team.Id,
                    GoalsWon = 0,
                    GoalsLost = 0
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await _publisher.Publish(new MatchesPlanned(tournament.Id, planned.Count), ct);
        return planned.Count;
    }
}
