using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public enum SeedingType { Random, Equality, Fair }

public record PlanSeveralMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId
) : IRequest<List<TournamentMatch>>;

public class PlanSeveralMatchesHandler : IRequestHandler<PlanSeveralMatchesCommand, List<TournamentMatch>>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public PlanSeveralMatchesHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<List<TournamentMatch>> Handle(PlanSeveralMatchesCommand request, CancellationToken ct)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, ct)
            ?? throw new InvalidOperationException("Tournament not found");

        List<TournamentPlayer> orderedPlayers;

        if (request.SeedTournamentId.HasValue && request.SeedingType != SeedingType.Random)
        {
            var seedPlayers = await _db.TournamentPlayers
                .Where(tp => tp.TournamentId == request.SeedTournamentId.Value)
                .Include(tp => tp.User)
                .OrderByDescending(tp => tp.Score)
                .ToListAsync(ct);

            // Map seed players to current tournament players by username
            var currentPlayerMap = tournament.Players.ToDictionary(p => p.User.Username);
            orderedPlayers = seedPlayers
                .Where(sp => currentPlayerMap.ContainsKey(sp.User.Username))
                .Select(sp => currentPlayerMap[sp.User.Username])
                .ToList();

            // Add any current players not in the seed
            var unordered = tournament.Players.Where(p => !orderedPlayers.Contains(p)).ToList();
            orderedPlayers.AddRange(unordered);
        }
        else
        {
            orderedPlayers = tournament.Players.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        var matches = new List<TournamentMatch>();
        var teamSize = tournament.TeamSize;
        int n = orderedPlayers.Count;

        var pairings = request.SeedingType switch
        {
            SeedingType.Equality => GenerateEqualityPairings(orderedPlayers, teamSize, request.GamesPerPlayer),
            SeedingType.Fair => GenerateFairPairings(orderedPlayers, teamSize, request.GamesPerPlayer),
            _ => GenerateRandomPairings(orderedPlayers, teamSize, request.GamesPerPlayer)
        };

        var maxOrder = await _db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .MaxAsync(m => (int?)m.Order, ct) ?? 0;

        int orderCounter = maxOrder + 1;

        foreach (var (team1Players, team2Players) in pairings)
        {
            var team1 = await CreateTeam(team1Players, request.TournamentId, 1, ct);
            var team2 = await CreateTeam(team2Players, request.TournamentId, 2, ct);

            var match = new TournamentMatch
            {
                TournamentId = request.TournamentId,
                Order = orderCounter++,
                State = MatchState.Planned
            };
            _db.TournamentMatches.Add(match);
            await _db.SaveChangesAsync(ct);

            _db.TournamentTeamMatchResults.AddRange(
                new TournamentTeamMatchResult { MatchId = match.Id, TournamentId = request.TournamentId, TeamId = team1.Id },
                new TournamentTeamMatchResult { MatchId = match.Id, TournamentId = request.TournamentId, TeamId = team2.Id }
            );
            await _db.SaveChangesAsync(ct);

            matches.Add(match);
        }

        await _mediator.Publish(new SeveralMatchesPlanned(request.TournamentId, matches.Count), ct);

        return matches;
    }

    private static List<(List<TournamentPlayer>, List<TournamentPlayer>)> GenerateRandomPairings(
        List<TournamentPlayer> players, int teamSize, int gamesPerPlayer)
    {
        var pairings = new List<(List<TournamentPlayer>, List<TournamentPlayer>)>();
        int totalMatches = players.Count * gamesPerPlayer / (2 * teamSize);

        for (int i = 0; i < totalMatches; i++)
        {
            var shuffled = players.OrderBy(_ => Guid.NewGuid()).ToList();
            pairings.Add((shuffled.Take(teamSize).ToList(), shuffled.Skip(teamSize).Take(teamSize).ToList()));
        }

        return pairings;
    }

    private static List<(List<TournamentPlayer>, List<TournamentPlayer>)> GenerateEqualityPairings(
        List<TournamentPlayer> players, int teamSize, int gamesPerPlayer)
    {
        // Best with worst: 1+N, 2+(N-1), ...
        var pairings = new List<(List<TournamentPlayer>, List<TournamentPlayer>)>();
        int n = players.Count;
        int matchCount = n / 2;

        for (int round = 0; round < gamesPerPlayer; round++)
        {
            for (int i = 0; i < matchCount; i++)
            {
                var team1 = new List<TournamentPlayer> { players[i] };
                var team2 = new List<TournamentPlayer> { players[n - 1 - i] };
                pairings.Add((team1, team2));
            }
        }

        return pairings;
    }

    private static List<(List<TournamentPlayer>, List<TournamentPlayer>)> GenerateFairPairings(
        List<TournamentPlayer> players, int teamSize, int gamesPerPlayer)
    {
        // Top half vs bottom half, pairing best of top with best of bottom
        var pairings = new List<(List<TournamentPlayer>, List<TournamentPlayer>)>();
        int n = players.Count;
        int half = n / 2;

        for (int round = 0; round < gamesPerPlayer; round++)
        {
            for (int i = 0; i < half; i++)
            {
                var team1 = new List<TournamentPlayer> { players[i] };
                var team2 = new List<TournamentPlayer> { players[half + i] };
                pairings.Add((team1, team2));
            }
        }

        return pairings;
    }

    private async Task<TournamentTeam> CreateTeam(
        List<TournamentPlayer> players, Guid tournamentId, int teamNumber, CancellationToken ct)
    {
        var team = new TournamentTeam
        {
            TournamentId = tournamentId,
            Number = teamNumber,
            Name = $"Team {teamNumber}"
        };
        _db.TournamentTeams.Add(team);
        await _db.SaveChangesAsync(ct);

        foreach (var player in players)
        {
            _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
            {
                TournamentTeamId = team.Id,
                TournamentPlayerId = player.Id
            });
        }
        await _db.SaveChangesAsync(ct);
        return team;
    }
}

public record SeveralMatchesPlanned(Guid TournamentId, int Count) : INotification;
