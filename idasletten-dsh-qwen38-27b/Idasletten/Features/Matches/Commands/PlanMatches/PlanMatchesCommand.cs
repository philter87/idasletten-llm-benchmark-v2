using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Matches.Events;
using Idasletten.Models;
using Idasletten.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.PlanMatches;

/// <summary>
/// Plans several matches at once.
/// - Seeding type decides the pairing: Random, Equality (best vs worst: 1+N, 2+(N-1), …)
///   or Fair (top half vs bottom half, best of top with best of bottom).
/// - FixedTeams: team compositions persist across all planned matches — the groups
///   play a round-robin. Otherwise every match gets freshly composed teams.
/// - SeedTournamentId (optional) is the ranking source; a tournament can only be
///   seeded if it has no parent.
/// </summary>
public sealed record PlanMatchesCommand(
    Guid TournamentId,
    Guid? SeedTournamentId,
    bool ApplySeedSelection,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType) : IRequest<int>;

public sealed class PlanMatchesCommandHandler : IRequestHandler<PlanMatchesCommand, int>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public PlanMatchesCommandHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<int> Handle(PlanMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new FeatureException("Tournament not found.");
        if (tournament.IsArchived)
            throw new FeatureException("This tournament is archived; matches can no longer be planned.");
        if (tournament.ParentTournamentId is not null)
            throw new FeatureException("A tournament with a parent round cannot be seeded; plan matches in the round instead.");

        if (request.GamesPerPlayer < 1 || request.GamesPerPlayer > 100)
            throw new FeatureException("Games per player must be between 1 and 100.");

        // Seed handling (only meaningful for the ranking; stored on the tournament).
        if (request.ApplySeedSelection)
        {
            if (request.SeedTournamentId is Guid seedId)
            {
                if (seedId == tournament.Id)
                    throw new FeatureException("A tournament cannot seed itself.");
                var seed = await _db.Tournaments.FirstOrDefaultAsync(t => t.Id == seedId, cancellationToken)
                    ?? throw new FeatureException("Seed tournament not found.");
                tournament.SeedTournamentId = seed.Id;
            }
            else
            {
                tournament.SeedTournamentId = null;
            }
        }

        var players = await _db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id)
            .ToListAsync(cancellationToken);

        // Eliminated players (Lives system) cannot be scheduled.
        if (tournament.ScoreSystem == ScoreSystem.Lives)
            players = players.Where(p => p.Lives > 0).ToList();

        var ts = tournament.TeamSize;
        var n = players.Count;
        if (n < 2 * ts)
            throw new FeatureException($"This tournament needs at least {2 * ts} players to plan matches (currently {n}).");

        var matchCount = n * request.GamesPerPlayer / (2 * ts);
        if (matchCount < 1)
            throw new FeatureException("Not enough players for that many games per player.");

        var ranking = await RankAsync(tournament, players, cancellationToken);

        int created;
        if (request.FixedTeams)
            created = await ScheduleFixedAsync(tournament, players, ranking, request, cancellationToken);
        else
            created = await ScheduleReshuffleAsync(tournament, players, ranking, request, cancellationToken);

        if (created == 0)
            throw new FeatureException("Could not plan any matches.");

        await _publisher.Publish(new MatchesPlanned(tournament.Id, created), cancellationToken);
        return created;
    }

    /// <summary>Ranking source: the seed tournament's scores when set (players missing
    /// there sort last), otherwise the current tournament's scores.</summary>
    private async Task<List<TournamentPlayer>> RankAsync(Tournament tournament, List<TournamentPlayer> players, CancellationToken ct)
    {
        var seedScores = new Dictionary<Guid, double>();
        if (tournament.SeedTournamentId is Guid seedId)
        {
            var seeded = await _db.TournamentPlayers
                .Where(p => p.TournamentId == seedId)
                .ToDictionaryAsync(p => p.UserId, p => (double)p.Score, ct);
            foreach (var p in players)
                if (seeded.TryGetValue(p.UserId, out var s))
                    seedScores[p.UserId] = s;
        }

        return players
            .OrderByDescending(p => seedScores.TryGetValue(p.UserId, out var s) ? s : -double.MaxValue)
            .ThenByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ThenBy(p => p.PointsLost)
            .ThenBy(p => p.User.Username)
            .ToList();
    }

    // ---------- reshuffle: fresh teams per match ----------

    private async Task<int> ScheduleReshuffleAsync(
        Tournament tournament, List<TournamentPlayer> players, List<TournamentPlayer> ranking,
        PlanMatchesCommand request, CancellationToken ct)
    {
        var ts = tournament.TeamSize;
        var n = players.Count;
        var pairings = new List<(List<Guid> A, List<Guid> B)>();

        if (request.SeedingType == SeedingType.Random)
        {
            var pool = new List<TournamentPlayer>();
            foreach (var p in players)
                for (var i = 0; i < request.GamesPerPlayer; i++) pool.Add(p);
            Shuffle(pool);
            for (var i = 0; i + 2 * ts <= pool.Count; i += 2 * ts)
            {
                var chunk = pool.GetRange(i, 2 * ts);
                Shuffle(chunk);
                pairings.Add((chunk.Take(ts).Select(p => p.Id).ToList(), chunk.Skip(ts).Select(p => p.Id).ToList()));
            }
        }
        else
        {
            if (n % ts != 0)
                throw new FeatureException($"With equality/fair seeding the player count ({n}) must be a multiple of the team size ({ts}).");
            if (request.SeedingType == SeedingType.Fair && n % (2 * ts) != 0)
                throw new FeatureException($"With fair seeding the player count ({n}) must be a multiple of {2 * ts} players.");

            for (var cycle = 0; cycle < request.GamesPerPlayer; cycle++)
            {
                if (request.SeedingType == SeedingType.Equality)
                {
                    // Best vs worst with a per-cycle rotation so cycles meet new opponents.
                    var rot = Rotate(ranking, cycle);
                    for (var i = 0; i < n / 2; i++)
                    {
                        var a = rot.GetRange(i * ts, ts);
                        var b = rot.GetRange(n - (i + 1) * ts, ts);
                        pairings.Add((a.Select(p => p.Id).ToList(), b.Select(p => p.Id).ToList()));
                    }
                }
                else // Fair: top half vs bottom half (best of top with best of bottom).
                {
                    var half = n / 2;
                    var top = ranking.GetRange(0, half);
                    var bottom = Rotate(ranking.GetRange(half, half), cycle);
                    for (var i = 0; i + ts <= half; i += ts)
                        pairings.Add((top.GetRange(i, ts).Select(p => p.Id).ToList(),
                                      bottom.GetRange(i, ts).Select(p => p.Id).ToList()));
                }
            }
        }

        return await CreatePlannedMatchesAsync(tournament, pairings, ct, fixedTeams: false);
    }

    // ---------- fixed: persistent groups, round-robin ----------

    private async Task<int> ScheduleFixedAsync(
        Tournament tournament, List<TournamentPlayer> players, List<TournamentPlayer> ranking,
        PlanMatchesCommand request, CancellationToken ct)
    {
        var ts = tournament.TeamSize;
        var n = players.Count;
        if (n % ts != 0)
            throw new FeatureException($"With fixed teams the player count ({n}) must be a multiple of the team size ({ts}).");

        // Groups: random shuffles for Random seeding, contiguous rank blocks otherwise
        // (Team 1 = strongest …).
        var source = request.SeedingType == SeedingType.Random ? ShuffleCopy(players) : ranking;
        var groups = new List<List<Guid>>();
        for (var i = 0; i + ts <= source.Count; i += ts)
            groups.Add(source.GetRange(i, ts).Select(p => p.Id).ToList());

        var m = groups.Count;
        var pairs = new List<(int I, int J)>();
        for (var i = 0; i < m; i++)
            for (var j = i + 1; j < m; j++)
                pairs.Add((i, j));

        var target = n * request.GamesPerPlayer / (2 * ts);
        var cycles = Math.Max(1, (int)Math.Round((double)target / Math.Max(1, pairs.Count)));

        var pairings = new List<(List<Guid> A, List<Guid> B)>();
        for (var c = 0; c < cycles; c++)
            foreach (var (i, j) in pairs)
                pairings.Add((groups[i], groups[j]));

        return await CreatePlannedMatchesAsync(tournament, pairings, ct, fixedTeams: true);
    }

    private async Task<int> CreatePlannedMatchesAsync(
        Tournament tournament, List<(List<Guid> A, List<Guid> B)> pairings,
        CancellationToken ct, bool fixedTeams)
    {
        var order = await MatchSupport.NextOrderAsync(_db, tournament.Id, ct);

        // Fixed mode: create each distinct group team once, then reuse across matches.
        var teamByGroupKey = new Dictionary<string, TournamentTeam>();
        if (fixedTeams)
        {
            var distinct = new List<List<Guid>>();
            foreach (var (a, _) in pairings)
                if (!distinct.Any(d => d.OrderBy(x => x).SequenceEqual(a.OrderBy(x => x))))
                    distinct.Add(a);
            for (var i = 0; i < distinct.Count; i++)
            {
                var team = await MatchSupport.CreateTeamAsync(_db, tournament, i + 1, distinct[i], ct);
                teamByGroupKey[Key(distinct[i])] = team;
            }
        }

        var created = 0;
        foreach (var (a, b) in pairings)
        {
            var match = new TournamentMatch
            {
                TournamentId = tournament.Id,
                Order = order++,
                State = MatchState.Planned
            };
            _db.TournamentMatches.Add(match);
            await _db.SaveChangesAsync(ct);

            for (var num = 1; num <= 2; num++)
            {
                var group = num == 1 ? a : b;
                TournamentTeam team = fixedTeams
                    ? teamByGroupKey[Key(group)]
                    : await MatchSupport.CreateTeamAsync(_db, tournament, num, group, ct);
                _db.MatchTeams.Add(new MatchTeam { MatchId = match.Id, TeamId = team.Id });
            }
            await _db.SaveChangesAsync(ct);
            created++;
        }
        return created;
    }

    private static string Key(List<Guid> ids) => string.Join(",", ids.OrderBy(x => x));

    private void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static List<TournamentPlayer> ShuffleCopy(List<TournamentPlayer> list)
    {
        var copy = list.ToList();
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy;
    }

    private static List<T> Rotate<T>(List<T> list, int by)
    {
        if (by == 0 || list.Count == 0) return list.ToList();
        var k = ((by % list.Count) + list.Count) % list.Count;
        return list.GetRange(k, list.Count - k).Concat(list.GetRange(0, k)).ToList();
    }
}
