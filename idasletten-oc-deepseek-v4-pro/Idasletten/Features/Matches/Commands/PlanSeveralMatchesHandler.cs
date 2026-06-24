using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class PlanSeveralMatchesHandler : IRequestHandler<PlanSeveralMatchesCommand, int>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public PlanSeveralMatchesHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<int> Handle(PlanSeveralMatchesCommand command, CancellationToken ct)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == command.TournamentId, ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var players = tournament.Players.ToList();
        if (players.Count < 2) return 0;

        var orderedPlayers = GetOrderedPlayers(command, players);
        var totalMatches = (int)Math.Ceiling(players.Count * command.GamesPerPlayer / 2.0);
        var matchesCreated = 0;

        for (int i = 0; i < totalMatches; i++)
        {
            var (team1Players, team2Players) = PickTeams(orderedPlayers, command);

            var match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = command.TournamentId,
                Order = await _db.TournamentMatches.CountAsync(m => m.TournamentId == command.TournamentId, ct) + 1,
                State = MatchState.Planned,
                CreatedAt = DateTime.UtcNow
            };
            _db.TournamentMatches.Add(match);
            matchesCreated++;

            var team1 = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                TournamentId = command.TournamentId,
                Name = $"Team {matchesCreated * 2 - 1}",
                Number = matchesCreated * 2 - 1
            };
            var team2 = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                TournamentId = command.TournamentId,
                Name = $"Team {matchesCreated * 2}",
                Number = matchesCreated * 2
            };
            _db.TournamentTeams.AddRange(team1, team2);

            foreach (var p in team1Players)
                _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer { TournamentTeamId = team1.Id, TournamentPlayerId = p.Id });
            foreach (var p in team2Players)
                _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer { TournamentTeamId = team2.Id, TournamentPlayerId = p.Id });
        }

        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new SeveralMatchesPlanned(command.TournamentId, matchesCreated), ct);
        return matchesCreated;
    }

    private List<TournamentPlayer> GetOrderedPlayers(PlanSeveralMatchesCommand command, List<TournamentPlayer> players)
    {
        if (command.SeedTournamentId.HasValue)
        {
            var seedPlayers = players.OrderByDescending(p => p.Score).ToList();
            return command.SeedingType switch
            {
                SeedingType.Equality => OrderEquality(seedPlayers),
                SeedingType.Fair => OrderFair(seedPlayers),
                _ => Shuffle(seedPlayers)
            };
        }

        return command.SeedingType switch
        {
            SeedingType.Equality => OrderEquality(players.OrderByDescending(p => p.Score).ToList()),
            SeedingType.Fair => OrderFair(players.OrderByDescending(p => p.Score).ToList()),
            _ => Shuffle(players)
        };
    }

    private static List<TournamentPlayer> OrderEquality(List<TournamentPlayer> sorted)
    {
        var result = new List<TournamentPlayer>();
        var count = sorted.Count;
        for (int i = 0; i < count / 2; i++)
        {
            result.Add(sorted[i]);
            result.Add(sorted[count - 1 - i]);
        }
        if (count % 2 == 1) result.Add(sorted[count / 2]);
        return result;
    }

    private static List<TournamentPlayer> OrderFair(List<TournamentPlayer> sorted)
    {
        var count = sorted.Count;
        var half = (count + 1) / 2;
        var result = new List<TournamentPlayer>();
        for (int i = 0; i < count / 2; i++)
        {
            result.Add(sorted[i]);
            result.Add(sorted[half + i]);
        }
        if (count % 2 == 1) result.Add(sorted[half - 1]);
        return result;
    }

    private static List<TournamentPlayer> Shuffle(List<TournamentPlayer> players)
    {
        var rng = new Random();
        return players.OrderBy(_ => rng.Next()).ToList();
    }

    private static (List<TournamentPlayer>, List<TournamentPlayer>) PickTeams(
        List<TournamentPlayer> ordered, PlanSeveralMatchesCommand command)
    {
        var teamSize = Math.Max(1, 2); // Default 2 per team
        var team1 = ordered.Take(teamSize).ToList();
        var team2 = ordered.Skip(teamSize).Take(teamSize).ToList();

        if (team2.Count < teamSize) team2.Add(team1[0]); // fallback

        if (!command.FixedTeams)
        {
            // Rotate for next match
            var first = ordered[0];
            ordered.RemoveAt(0);
            ordered.Add(first);
        }

        return (team1, team2);
    }
}
