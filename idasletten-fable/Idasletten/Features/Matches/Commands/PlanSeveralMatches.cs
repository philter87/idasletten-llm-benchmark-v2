using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public enum SeedingType
{
    /// <summary>Teams chosen randomly.</summary>
    Random,

    /// <summary>Pair best with worst: 1+N, 2+(N-1), ...</summary>
    Equality,

    /// <summary>Split ranked players in a top and bottom half, pair best of each half: 10 players → 1+6, 2+7, ...</summary>
    Fair
}

public record PlanSeveralMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId = null) : IRequest<List<TournamentMatch>>;

public record MatchesPlanned(Guid TournamentId, List<Guid> MatchIds) : INotification;

public class PlanSeveralMatchesHandler(AppDbContext db, IMediator mediator, IPublisher publisher)
    : IRequestHandler<PlanSeveralMatchesCommand, List<TournamentMatch>>
{
    public async Task<List<TournamentMatch>> Handle(PlanSeveralMatchesCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        if (request.SeedTournamentId is Guid seedId && tournament.SeedTournamentId is null)
            await mediator.Send(new SetSeedTournamentCommand(tournament.Id, seedId), ct);

        var players = await db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id)
            .ToListAsync(ct);

        var teamSize = Math.Max(1, tournament.TeamSize);
        if (players.Count < teamSize * 2)
            throw new InvalidOperationException(
                $"Planning needs at least {teamSize * 2} players in the tournament; there are {players.Count}.");

        var ranked = await RankPlayers(tournament, players, ct);

        var totalMatches = (int)Math.Ceiling(
            request.GamesPerPlayer * players.Count / (double)(teamSize * 2));

        var maxOrder = await db.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .Select(m => (int?)m.Order)
            .MaxAsync(ct) ?? 0;

        var matches = new List<TournamentMatch>();
        var random = new Random();
        List<List<TournamentPlayer>>? fixedTeamCompositions = null;
        var round = 0;

        while (matches.Count < totalMatches)
        {
            List<List<TournamentPlayer>> compositions;
            if (request.FixedTeams)
            {
                fixedTeamCompositions ??= BuildTeams(ranked, teamSize, request.SeedingType, random);
                compositions = fixedTeamCompositions;
            }
            else
            {
                compositions = BuildTeams(ranked, teamSize, request.SeedingType, random);
            }

            var teams = new List<TournamentTeam>();
            foreach (var composition in compositions)
                teams.Add(await TeamResolver.FindOrCreateTeam(db, tournament, composition, ct));

            // Rotate pairings each round so fixed teams still meet different opponents.
            var rotated = Rotate(teams, round);
            for (var i = 0; i + 1 < rotated.Count && matches.Count < totalMatches; i += 2)
            {
                var match = new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    Order = ++maxOrder,
                    State = MatchState.Planned,
                    Results =
                    [
                        new TournamentTeamMatchResult { TournamentId = tournament.Id, TeamId = rotated[i].Id, Team = rotated[i] },
                        new TournamentTeamMatchResult { TournamentId = tournament.Id, TeamId = rotated[i + 1].Id, Team = rotated[i + 1] }
                    ]
                };
                db.TournamentMatches.Add(match);
                matches.Add(match);
            }
            round++;
        }

        await db.SaveChangesAsync(ct);
        await publisher.Publish(new MatchesPlanned(tournament.Id, matches.Select(m => m.Id).ToList()), ct);
        return matches;
    }

    /// <summary>
    /// Ranks players best-first. When a seed tournament is set, its scores are used;
    /// players unknown to the seed tournament rank last.
    /// </summary>
    private async Task<List<TournamentPlayer>> RankPlayers(
        Tournament tournament, List<TournamentPlayer> players, CancellationToken ct)
    {
        if (tournament.SeedTournamentId is Guid seedId)
        {
            var seedScores = await db.TournamentPlayers
                .Where(p => p.TournamentId == seedId)
                .ToDictionaryAsync(p => p.UserId, p => p.Score, ct);
            return players
                .OrderByDescending(p => seedScores.TryGetValue(p.UserId, out var score) ? score : double.MinValue)
                .ThenByDescending(p => p.Score)
                .ToList();
        }

        return players
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ToList();
    }

    /// <summary>Splits ranked players (best first) into teams of the requested size.</summary>
    public static List<List<TournamentPlayer>> BuildTeams(
        List<TournamentPlayer> ranked, int teamSize, SeedingType seedingType, Random random)
    {
        var teamCount = ranked.Count / teamSize;
        var usable = ranked.Take(teamCount * teamSize).ToList();

        switch (seedingType)
        {
            case SeedingType.Random:
            {
                var shuffled = ranked.OrderBy(_ => random.Next()).Take(teamCount * teamSize).ToList();
                return Enumerable.Range(0, teamCount)
                    .Select(i => shuffled.Skip(i * teamSize).Take(teamSize).ToList())
                    .ToList();
            }
            case SeedingType.Equality:
            {
                // Best with worst: repeatedly take one from the front and fill up from the back.
                var teams = Enumerable.Range(0, teamCount).Select(_ => new List<TournamentPlayer>()).ToList();
                var front = 0;
                var back = usable.Count - 1;
                for (var i = 0; i < teamCount; i++)
                {
                    teams[i].Add(usable[front++]);
                    while (teams[i].Count < teamSize)
                        teams[i].Add(usable[back--]);
                }
                return teams;
            }
            case SeedingType.Fair:
            {
                // Split into <teamSize> ranked groups, team i takes the i-th player of each group:
                // 10 players, size 2 → 1+6, 2+7, 3+8, 4+9, 5+10.
                var teams = Enumerable.Range(0, teamCount).Select(_ => new List<TournamentPlayer>()).ToList();
                for (var g = 0; g < teamSize; g++)
                    for (var i = 0; i < teamCount; i++)
                        teams[i].Add(usable[g * teamCount + i]);
                return teams;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(seedingType));
        }
    }

    private static List<T> Rotate<T>(List<T> items, int offset)
    {
        if (items.Count == 0)
            return items;
        var shift = offset % items.Count;
        return items.Skip(shift).Concat(items.Take(shift)).ToList();
    }
}
