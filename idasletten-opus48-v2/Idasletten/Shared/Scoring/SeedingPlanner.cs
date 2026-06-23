using Idasletten.Shared.Domain;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Builds planned matches from a set of players according to a <see cref="SeedingType"/>.
/// A match is a list of teams; a team is a list of user ids. Optimised for the common
/// two-player team case while degrading gracefully for other team sizes.
/// </summary>
public static class SeedingPlanner
{
    public static List<List<List<Guid>>> Plan(
        IReadOnlyList<(Guid UserId, double SeedScore)> players,
        int teamSize, int gamesPerPlayer, bool fixedTeam, SeedingType seeding, Random rng)
    {
        var matches = new List<List<List<Guid>>>();
        if (players.Count < teamSize * 2 || gamesPerPlayer < 1) return matches;

        List<List<Guid>>? fixedTeams = null;

        for (int round = 0; round < gamesPerPlayer; round++)
        {
            var teams = fixedTeam && fixedTeams is not null
                ? Rotate(fixedTeams, round)
                : FormTeams(players, teamSize, seeding, rng);

            if (fixedTeam && fixedTeams is null) fixedTeams = teams;

            // Pair consecutive teams into matches; drop a trailing unpaired team.
            for (int i = 0; i + 1 < teams.Count; i += 2)
                matches.Add(new List<List<Guid>> { teams[i], teams[i + 1] });
        }

        return matches;
    }

    private static List<List<Guid>> FormTeams(
        IReadOnlyList<(Guid UserId, double SeedScore)> players, int teamSize, SeedingType seeding, Random rng)
    {
        List<Guid> ordered = seeding switch
        {
            SeedingType.Random => players.OrderBy(_ => rng.Next()).Select(p => p.UserId).ToList(),
            _ => players.OrderByDescending(p => p.SeedScore).Select(p => p.UserId).ToList()
        };

        var teams = new List<List<Guid>>();

        if (teamSize == 2 && seeding != SeedingType.Random)
        {
            if (seeding == SeedingType.Equality)
            {
                // Best with worst: 1+N, 2+(N-1), ...
                int lo = 0, hi = ordered.Count - 1;
                while (lo < hi)
                {
                    teams.Add(new List<Guid> { ordered[lo], ordered[hi] });
                    lo++; hi--;
                }
            }
            else // Fair: best of top half with best of bottom half
            {
                int half = ordered.Count / 2;
                var top = ordered.Take(half).ToList();
                var bottom = ordered.Skip(half).ToList();
                for (int i = 0; i < half && i < bottom.Count; i++)
                    teams.Add(new List<Guid> { top[i], bottom[i] });
            }
            return teams;
        }

        // General case: sequential chunks of teamSize.
        for (int i = 0; i + teamSize <= ordered.Count; i += teamSize)
            teams.Add(ordered.GetRange(i, teamSize));
        return teams;
    }

    private static List<List<Guid>> Rotate(List<List<Guid>> teams, int by)
    {
        if (teams.Count == 0) return teams;
        by %= teams.Count;
        return teams.Skip(by).Concat(teams.Take(by)).ToList();
    }
}
