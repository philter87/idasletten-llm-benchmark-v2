using Idasletten.Features.Matches.Entities;
using Idasletten.Features.Matches.Events;
using Idasletten.Features.Tournaments.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public enum SeedingType
{
    Random,
    Equality,
    Fair
}

public record PlanSeveralMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId = null) : IRequest<IReadOnlyList<Guid>>;

public sealed class PlanSeveralMatchesHandler(AppDbContext db, IMediator mediator) : IRequestHandler<PlanSeveralMatchesCommand, IReadOnlyList<Guid>>
{
    private readonly AppDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<IReadOnlyList<Guid>> Handle(PlanSeveralMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == request.TournamentId, cancellationToken);

        if (tournament is null)
        {
            throw new InvalidOperationException($"Tournament '{request.TournamentId}' was not found.");
        }

        if (request.GamesPerPlayer <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.GamesPerPlayer), "Games per player must be greater than zero.");
        }

        var players = await _db.TournamentPlayers
            .Where(player => player.TournamentId == tournament.Id)
            .Include(player => player.User)
            .ToListAsync(cancellationToken);

        var playersPerMatch = tournament.TeamSize * 2;
        if (players.Count < playersPerMatch)
        {
            throw new InvalidOperationException($"At least {playersPerMatch} players are required to plan a match.");
        }

        var totalMatches = (players.Count * request.GamesPerPlayer) / playersPerMatch;
        if (totalMatches <= 0)
        {
            return Array.Empty<Guid>();
        }

        var rankingScores = await BuildRankingDictionaryAsync(tournament, request.SeedTournamentId, players, cancellationToken);
        var rankedPlayers = players
            .OrderByDescending(player => rankingScores.GetValueOrDefault(player.UserId, player.Score))
            .ThenBy(player => player.User.Username)
            .ToList();

        var nextOrder = (await _db.TournamentMatches
            .Where(match => match.TournamentId == tournament.Id)
            .Select(match => (int?)match.Order)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var nextTeamNumber = (await _db.TournamentTeams
            .Where(team => team.TournamentId == tournament.Id)
            .Select(team => (int?)team.Number)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var fixedRandomOrder = request.FixedTeams && request.SeedingType == SeedingType.Random
            ? ShuffleCopy(rankedPlayers)
            : null;

        var plannedMatches = new List<TournamentMatch>();
        var createdMatchIds = new List<Guid>();

        for (var round = 0; plannedMatches.Count < totalMatches; round++)
        {
            List<TournamentPlayer> seededOrder = request.SeedingType switch
            {
                SeedingType.Random => fixedRandomOrder is not null ? fixedRandomOrder.ToList() : ShuffleCopy(rankedPlayers),
                SeedingType.Equality => BuildEqualityOrder(rankedPlayers, request.FixedTeams ? 0 : round * tournament.TeamSize),
                SeedingType.Fair => BuildFairOrder(rankedPlayers, request.FixedTeams ? 0 : round * tournament.TeamSize),
                _ => rankedPlayers.ToList()
            };

            var teams = ChunkTeams(seededOrder, tournament.TeamSize);
            var roundMatches = PairTeams(teams);

            if (roundMatches.Count == 0)
            {
                break;
            }

            foreach (var roundMatch in roundMatches)
            {
                if (plannedMatches.Count == totalMatches)
                {
                    break;
                }

                var match = new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    Order = nextOrder++,
                    State = MatchState.Planned
                };

                _db.TournamentMatches.Add(match);
                plannedMatches.Add(match);
                createdMatchIds.Add(match.Id);

                foreach (var teamPlayers in roundMatch)
                {
                    var teamEntity = new TournamentTeam
                    {
                        TournamentId = tournament.Id,
                        Number = nextTeamNumber,
                        Name = $"Team {nextTeamNumber}"
                    };

                    nextTeamNumber += 1;
                    _db.TournamentTeams.Add(teamEntity);

                    foreach (var player in teamPlayers)
                    {
                        _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
                        {
                            Team = teamEntity,
                            TournamentPlayerId = player.Id
                        });
                    }

                    _db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
                    {
                        Match = match,
                        TournamentId = tournament.Id,
                        Team = teamEntity,
                        GoalsWon = 0,
                        GoalsLost = 0
                    });
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var match in plannedMatches)
        {
            await _mediator.Publish(new MatchPlanned(match.Id, tournament.Id), cancellationToken);
        }

        return createdMatchIds;
    }

    private async Task<Dictionary<Guid, double>> BuildRankingDictionaryAsync(
        Tournament tournament,
        Guid? seedTournamentId,
        IReadOnlyList<TournamentPlayer> currentPlayers,
        CancellationToken cancellationToken)
    {
        var rankingTournamentId = seedTournamentId ?? tournament.SeedTournamentId;
        if (!rankingTournamentId.HasValue)
        {
            return currentPlayers.ToDictionary(player => player.UserId, player => player.Score);
        }

        var seedScores = await _db.TournamentPlayers
            .AsNoTracking()
            .Where(player => player.TournamentId == rankingTournamentId.Value)
            .ToDictionaryAsync(player => player.UserId, player => player.Score, cancellationToken);

        return currentPlayers.ToDictionary(
            player => player.UserId,
            player => seedScores.GetValueOrDefault(player.UserId, player.Score));
    }

    private static List<List<TournamentPlayer>> ChunkTeams(IReadOnlyList<TournamentPlayer> players, int teamSize)
    {
        var teams = new List<List<TournamentPlayer>>();

        for (var index = 0; index + teamSize <= players.Count; index += teamSize)
        {
            teams.Add(players.Skip(index).Take(teamSize).ToList());
        }

        return teams;
    }

    private static List<TournamentPlayer> BuildEqualityOrder(IReadOnlyList<TournamentPlayer> rankedPlayers, int rotation)
    {
        var ordered = new List<TournamentPlayer>(rankedPlayers.Count);
        var left = 0;
        var right = rankedPlayers.Count - 1;

        while (left <= right)
        {
            ordered.Add(rankedPlayers[left]);
            if (left != right)
            {
                ordered.Add(rankedPlayers[right]);
            }

            left += 1;
            right -= 1;
        }

        return Rotate(ordered, rotation);
    }

    private static List<TournamentPlayer> BuildFairOrder(IReadOnlyList<TournamentPlayer> rankedPlayers, int rotation)
    {
        var ordered = new List<TournamentPlayer>(rankedPlayers.Count);
        var splitIndex = (int)Math.Ceiling(rankedPlayers.Count / 2d);
        var topHalf = rankedPlayers.Take(splitIndex).ToList();
        var bottomHalf = rankedPlayers.Skip(splitIndex).ToList();
        var pairCount = Math.Max(topHalf.Count, bottomHalf.Count);

        for (var index = 0; index < pairCount; index++)
        {
            if (index < topHalf.Count)
            {
                ordered.Add(topHalf[index]);
            }

            if (index < bottomHalf.Count)
            {
                ordered.Add(bottomHalf[index]);
            }
        }

        return Rotate(ordered, rotation);
    }

    private static List<IReadOnlyList<IReadOnlyList<TournamentPlayer>>> PairTeams(IReadOnlyList<List<TournamentPlayer>> teams)
    {
        var matches = new List<IReadOnlyList<IReadOnlyList<TournamentPlayer>>>();

        for (var index = 0; index + 1 < teams.Count; index += 2)
        {
            matches.Add(new List<IReadOnlyList<TournamentPlayer>>
            {
                teams[index],
                teams[index + 1]
            });
        }

        return matches;
    }

    private static List<TournamentPlayer> Rotate(IReadOnlyList<TournamentPlayer> players, int rotation)
    {
        if (players.Count == 0)
        {
            return new List<TournamentPlayer>();
        }

        var normalizedRotation = ((rotation % players.Count) + players.Count) % players.Count;
        if (normalizedRotation == 0)
        {
            return players.ToList();
        }

        return players.Skip(normalizedRotation).Concat(players.Take(normalizedRotation)).ToList();
    }

    private static List<TournamentPlayer> ShuffleCopy(IReadOnlyList<TournamentPlayer> players)
    {
        var copy = players.ToList();

        for (var index = copy.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (copy[index], copy[swapIndex]) = (copy[swapIndex], copy[index]);
        }

        return copy;
    }
}
