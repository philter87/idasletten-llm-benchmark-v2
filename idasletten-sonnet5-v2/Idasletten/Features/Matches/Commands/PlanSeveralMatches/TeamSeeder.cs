using Idasletten.Shared.Entities;

namespace Idasletten.Features.Matches.Commands.PlanSeveralMatches;

/// <summary>Pure team-forming logic for "Plan several matches", split out for easy unit testing.</summary>
public static class TeamSeeder
{
    public static List<List<Guid>> BuildTeams(IReadOnlyList<Guid> rankedPlayerIds, SeedingType seedingType, int teamSize)
    {
        if (seedingType == SeedingType.Random)
        {
            var shuffled = rankedPlayerIds.OrderBy(_ => Random.Shared.Next()).ToList();
            return Chunk(shuffled, teamSize);
        }

        if (teamSize != 2)
        {
            // Equality/Fair are specified by example in terms of 2-player teams; for other
            // team sizes we fall back to simple ranked chunking.
            return Chunk(rankedPlayerIds.ToList(), teamSize);
        }

        var n = rankedPlayerIds.Count;
        var teams = new List<List<Guid>>();

        if (seedingType == SeedingType.Equality)
        {
            // Pair best with worst: 1+N, 2+(N-1), ...
            for (var i = 0; i < n / 2; i++)
            {
                teams.Add([rankedPlayerIds[i], rankedPlayerIds[n - 1 - i]]);
            }
        }
        else
        {
            // Fair: split into top/bottom half, pair best-of-top with best-of-bottom: 1+6, 2+7, ...
            var half = n / 2;
            for (var i = 0; i < half; i++)
            {
                teams.Add([rankedPlayerIds[i], rankedPlayerIds[half + i]]);
            }
        }

        return teams;
    }

    private static List<List<Guid>> Chunk(List<Guid> players, int teamSize)
    {
        var teams = new List<List<Guid>>();
        for (var i = 0; i + teamSize <= players.Count; i += teamSize)
        {
            teams.Add(players.Skip(i).Take(teamSize).ToList());
        }
        return teams;
    }
}
