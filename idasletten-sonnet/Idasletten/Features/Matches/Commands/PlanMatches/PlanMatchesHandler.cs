using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.PlanMatches;

public class PlanMatchesHandler(AppDbContext db) : IRequestHandler<PlanMatchesCommand, int>
{
    public async Task<int> Handle(PlanMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        var players = await db.TournamentPlayers
            .Include(tp => tp.User)
            .Where(tp => tp.TournamentId == request.TournamentId)
            .ToListAsync(cancellationToken);

        List<TournamentPlayer> orderedPlayers;

        if (request.SeedTournamentId.HasValue)
        {
            var seedPlayers = await db.TournamentPlayers
                .Where(tp => tp.TournamentId == request.SeedTournamentId.Value)
                .OrderByDescending(tp => tp.Score)
                .ToListAsync(cancellationToken);

            orderedPlayers = players
                .OrderBy(p => seedPlayers.FindIndex(sp => sp.UserId == p.UserId) is var idx && idx == -1 ? int.MaxValue : idx)
                .ToList();
        }
        else
        {
            orderedPlayers = players.OrderByDescending(p => p.Score).ToList();
        }

        var matchPairs = GeneratePairs(orderedPlayers, tournament.TeamSize, request.GamesPerPlayer, request.SeedingType);

        var currentOrder = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .CountAsync(cancellationToken);

        int created = 0;
        foreach (var (team1Users, team2Users) in matchPairs)
        {
            var match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = request.TournamentId,
                Order = ++currentOrder,
                State = MatchState.Planned,
            };

            var t1 = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                TournamentId = request.TournamentId,
                Number = 1,
                Name = "Team 1",
                Players = team1Users,
            };

            var t2 = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                TournamentId = request.TournamentId,
                Number = 2,
                Name = "Team 2",
                Players = team2Users,
            };

            db.TournamentMatches.Add(match);
            db.TournamentTeams.AddRange(t1, t2);

            db.TournamentTeamMatchResults.AddRange(
                new TournamentTeamMatchResult { Id = Guid.NewGuid(), MatchId = match.Id, TournamentId = request.TournamentId, TeamId = t1.Id },
                new TournamentTeamMatchResult { Id = Guid.NewGuid(), MatchId = match.Id, TournamentId = request.TournamentId, TeamId = t2.Id }
            );

            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static List<(List<TournamentPlayer>, List<TournamentPlayer>)> GeneratePairs(
        List<TournamentPlayer> players, int teamSize, int gamesPerPlayer, SeedingType seedingType)
    {
        var pairs = new List<(List<TournamentPlayer>, List<TournamentPlayer>)>();
        int n = players.Count;
        if (n < teamSize * 2) return pairs;

        var ordered = seedingType switch
        {
            SeedingType.Equality => players,
            SeedingType.Fair => players,
            _ => players.OrderBy(_ => Guid.NewGuid()).ToList()
        };

        var totalGames = (n * gamesPerPlayer) / (teamSize * 2);
        var rng = new Random();

        for (int g = 0; g < totalGames; g++)
        {
            if (seedingType == SeedingType.Equality)
            {
                // best with worst: 1+N, 2+(N-1)
                var half1 = new List<TournamentPlayer>();
                var half2 = new List<TournamentPlayer>();
                for (int i = 0; i < teamSize; i++)
                {
                    half1.Add(ordered[i % n]);
                    half2.Add(ordered[(n - 1 - i) % n]);
                }
                pairs.Add((half1, half2));
            }
            else if (seedingType == SeedingType.Fair)
            {
                // top half vs bottom half: 1+N/2+1, etc.
                int mid = n / 2;
                var half1 = new List<TournamentPlayer>();
                var half2 = new List<TournamentPlayer>();
                for (int i = 0; i < teamSize; i++)
                {
                    half1.Add(ordered[i % mid]);
                    half2.Add(ordered[mid + (i % (n - mid))]);
                }
                pairs.Add((half1, half2));
            }
            else
            {
                var shuffled = ordered.OrderBy(_ => rng.Next()).ToList();
                pairs.Add((shuffled.Take(teamSize).ToList(), shuffled.Skip(teamSize).Take(teamSize).ToList()));
            }
        }

        return pairs;
    }
}
