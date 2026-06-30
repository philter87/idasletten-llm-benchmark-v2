using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.PlanMultipleMatches;

/// <summary>
/// Generates `GamesPerPlayer` rounds of matches for the tournament's current player pool.
/// Each round forms teams from the full pool (dropping any remainder that doesn't fill a
/// full team/match) and pairs adjacent teams into matches. With FixedTeams, team rosters are
/// computed once and every round repeats the same match-ups; otherwise each round re-derives
/// rosters (a fresh shuffle for Random, a rotated ranking for Equality/Fair so teammates vary
/// round to round while still following the balancing rule).
/// </summary>
public class PlanMultipleMatchesHandler(IdaslettenDbContext db, IPublisher publisher)
    : IRequestHandler<PlanMultipleMatchesCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(PlanMultipleMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FirstAsync(t => t.Id == request.TournamentId, cancellationToken);
        var teamSize = tournament.TeamSize;

        var rankedPlayerIds = await GetRankedPlayerIdsAsync(request, cancellationToken);
        if (rankedPlayerIds.Count < teamSize * 2)
        {
            return [];
        }

        var random = new Random();
        var nextOrder = (await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .Select(m => (int?)m.Order)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        List<List<Guid>>? fixedTeams = null;
        var createdMatchIds = new List<Guid>();
        var matchesToAdd = new List<TournamentMatch>();

        for (var round = 0; round < request.GamesPerPlayer; round++)
        {
            var teams = request.FixedTeams
                ? fixedTeams ??= FormTeams(rankedPlayerIds, teamSize, request.SeedingType, random, 0)
                : FormTeams(rankedPlayerIds, teamSize, request.SeedingType, random, round);

            for (var i = 0; i + 1 < teams.Count; i += 2)
            {
                var match = new TournamentMatch
                {
                    Id = Guid.NewGuid(),
                    TournamentId = request.TournamentId,
                    Order = nextOrder++,
                    State = MatchState.Planned
                };

                AddTeam(match, teams[i], 1);
                AddTeam(match, teams[i + 1], 2);

                matchesToAdd.Add(match);
                createdMatchIds.Add(match.Id);
            }
        }

        db.TournamentMatches.AddRange(matchesToAdd);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new MatchesPlanned(request.TournamentId, createdMatchIds), cancellationToken);

        return createdMatchIds;
    }

    private static void AddTeam(TournamentMatch match, List<Guid> tournamentPlayerIds, int number)
    {
        var team = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            TournamentId = match.TournamentId,
            Number = number,
            Name = $"Team {number}"
        };
        foreach (var playerId in tournamentPlayerIds)
        {
            team.Players.Add(new TournamentTeamPlayer { TeamId = team.Id, TournamentPlayerId = playerId });
        }
        match.Teams.Add(team);
    }

    private async Task<List<Guid>> GetRankedPlayerIdsAsync(PlanMultipleMatchesCommand request, CancellationToken cancellationToken)
    {
        if (request.SeedingType == SeedingType.Random || request.SeedTournamentId is null)
        {
            return await db.TournamentPlayers
                .Where(p => p.TournamentId == request.TournamentId)
                .OrderByDescending(p => p.Score)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        // Rank current-tournament players by their Score in the seed tournament (matched by
        // UserId); players with no seed-tournament history are appended at the end.
        var currentPlayers = await db.TournamentPlayers
            .Where(p => p.TournamentId == request.TournamentId)
            .Select(p => new { p.Id, p.UserId })
            .ToListAsync(cancellationToken);

        var seedScores = await db.TournamentPlayers
            .Where(p => p.TournamentId == request.SeedTournamentId)
            .Select(p => new { p.UserId, p.Score })
            .ToDictionaryAsync(p => p.UserId, p => p.Score, cancellationToken);

        return currentPlayers
            .OrderByDescending(p => seedScores.TryGetValue(p.UserId, out var score) ? score : double.MinValue)
            .Select(p => p.Id)
            .ToList();
    }

    private static List<List<Guid>> FormTeams(List<Guid> rankedPlayerIds, int teamSize, SeedingType type, Random random, int rotation)
    {
        return type switch
        {
            SeedingType.Random => ChunkSequential(rankedPlayerIds.OrderBy(_ => random.Next()).ToList(), teamSize),
            SeedingType.Equality => PairBestWithWorst(Rotate(rankedPlayerIds, rotation), teamSize),
            SeedingType.Fair => PairTopHalfWithBottomHalf(Rotate(rankedPlayerIds, rotation), teamSize),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static List<Guid> Rotate(List<Guid> list, int positions)
    {
        if (list.Count == 0) return list;
        var shift = positions % list.Count;
        return list.Skip(shift).Concat(list.Take(shift)).ToList();
    }

    private static List<List<Guid>> ChunkSequential(List<Guid> players, int teamSize)
    {
        var teams = new List<List<Guid>>();
        for (var i = 0; i + teamSize <= players.Count; i += teamSize)
        {
            teams.Add(players.Skip(i).Take(teamSize).ToList());
        }
        return teams;
    }

    /// Best with worst: (rank1, rankN), (rank2, rank N-1), ...
    private static List<List<Guid>> PairBestWithWorst(List<Guid> ranked, int teamSize)
    {
        var teams = new List<List<Guid>>();
        var lo = 0;
        var hi = ranked.Count - 1;
        while (lo <= hi)
        {
            var team = new List<Guid>();
            var takeFromFront = true;
            while (team.Count < teamSize && lo <= hi)
            {
                team.Add(takeFromFront ? ranked[lo++] : ranked[hi--]);
                takeFromFront = !takeFromFront;
            }
            if (team.Count == teamSize) teams.Add(team);
        }
        return teams;
    }

    /// Top half paired with bottom half at matching positions: 1+6, 2+7, 3+8, ... (10 players).
    private static List<List<Guid>> PairTopHalfWithBottomHalf(List<Guid> ranked, int teamSize)
    {
        var teamsCount = ranked.Count / teamSize;
        if (teamsCount == 0) return [];

        var slices = Enumerable.Range(0, teamSize)
            .Select(s => ranked.Skip(s * teamsCount).Take(teamsCount).ToList())
            .ToList();

        var teams = new List<List<Guid>>();
        for (var i = 0; i < teamsCount; i++)
        {
            var team = slices.Select(slice => slice[i]).ToList();
            teams.Add(team);
        }
        return teams;
    }
}
