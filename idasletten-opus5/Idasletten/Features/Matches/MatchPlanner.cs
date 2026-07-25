namespace Idasletten.Features.Matches;

public enum SeedingType
{
    /// <summary>Teams are drawn randomly.</summary>
    Random = 0,

    /// <summary>Best is paired with worst: 1+N, 2+(N-1), ... so every team is equally strong.</summary>
    Equality = 1,

    /// <summary>Ranked players are split in a top and a bottom half: 1+6, 2+7, 3+8 for 10 players.</summary>
    Fair = 2,
}

public static class SeedingTypeInfo
{
    public static string Title(this SeedingType type) => type switch
    {
        SeedingType.Random => "Tilfældig",
        SeedingType.Equality => "Lighed",
        SeedingType.Fair => "Fair",
        _ => type.ToString(),
    };

    public static string Description(this SeedingType type) => type switch
    {
        SeedingType.Random => "Holdene trækkes tilfældigt - aserne bestemmer.",
        SeedingType.Equality =>
            "Den bedste spiller sættes sammen med den dårligste (1+N, 2+N-1, ...), så alle hold bliver lige stærke.",
        SeedingType.Fair =>
            "De rangerede spillere deles i en øverste og en nederste halvdel, og de bedste fra hver halvdel spiller sammen: med 10 spillere bliver det 1+6, 2+7, 3+8, 4+9 og 5+10.",
        _ => string.Empty,
    };
}

/// <summary>
/// Turns a ranked list of players into planned games. Pure and deterministic for a given
/// <see cref="Random"/>, which is what makes it testable without a database.
/// </summary>
public static class MatchPlanner
{
    public record PlannedTeam(IReadOnlyList<Guid> PlayerIds);

    public record PlannedGame(IReadOnlyList<PlannedTeam> Teams);

    /// <param name="rankedPlayerIds">Tournament player ids, best player first.</param>
    /// <param name="teamSize">Players per team.</param>
    /// <param name="gamesPerPlayer">How many games each player should get - one game per round.</param>
    /// <param name="fixedTeams">Keep the same teams for every round instead of reshuffling.</param>
    /// <param name="seeding">How players are put together in teams.</param>
    /// <param name="random">Source of randomness, injected so tests are deterministic.</param>
    public static IReadOnlyList<PlannedGame> Plan(
        IReadOnlyList<Guid> rankedPlayerIds,
        int teamSize,
        int gamesPerPlayer,
        bool fixedTeams,
        SeedingType seeding,
        Random random)
    {
        var games = new List<PlannedGame>();

        if (rankedPlayerIds.Count < teamSize * 2 || gamesPerPlayer < 1 || teamSize < 1)
        {
            return games;
        }

        List<PlannedTeam>? sharedTeams = null;

        for (var round = 0; round < gamesPerPlayer; round++)
        {
            var teams = fixedTeams
                ? sharedTeams ??= BuildTeams(OrderForRound(rankedPlayerIds, seeding, 0, random), teamSize, seeding)
                : BuildTeams(OrderForRound(rankedPlayerIds, seeding, round, random), teamSize, seeding);

            // Rotating the team list means new opponents every round, also for fixed teams.
            var pairingOrder = Rotate(teams, round);

            for (var i = 0; i + 1 < pairingOrder.Count; i += 2)
            {
                games.Add(new PlannedGame([pairingOrder[i], pairingOrder[i + 1]]));
            }
        }

        return games;
    }

    /// <summary>How many games <see cref="Plan"/> will produce - used to preview the plan in the UI.</summary>
    public static int GameCount(int playerCount, int teamSize, int gamesPerPlayer)
    {
        if (teamSize < 1 || gamesPerPlayer < 1)
        {
            return 0;
        }

        var teamsPerRound = playerCount / teamSize;
        return teamsPerRound / 2 * gamesPerPlayer;
    }

    private static List<Guid> OrderForRound(
        IReadOnlyList<Guid> rankedPlayerIds, SeedingType seeding, int round, Random random)
    {
        if (seeding == SeedingType.Random)
        {
            return Shuffle(rankedPlayerIds, random);
        }

        // Keeping the ranking but starting one player further in gives everybody new team mates
        // between the rounds, and lets a different player sit over when the count does not add up.
        return Rotate(rankedPlayerIds, round);
    }

    private static List<PlannedTeam> BuildTeams(
        List<Guid> orderedPlayerIds, int teamSize, SeedingType seeding) => seeding switch
    {
        SeedingType.Equality => BuildEqualityTeams(orderedPlayerIds, teamSize),
        SeedingType.Fair => BuildFairTeams(orderedPlayerIds, teamSize),
        _ => Chunk(orderedPlayerIds, teamSize),
    };

    /// <summary>Sequential chunks - used for random seeding where the list is already shuffled.</summary>
    private static List<PlannedTeam> Chunk(List<Guid> players, int teamSize) =>
        players
            .Chunk(teamSize)
            .Where(chunk => chunk.Length == teamSize)
            .Select(chunk => new PlannedTeam(chunk))
            .ToList();

    /// <summary>Best with worst: takes players alternately from the top and the bottom of the ranking.</summary>
    private static List<PlannedTeam> BuildEqualityTeams(List<Guid> ranked, int teamSize)
    {
        var pool = new List<Guid>(ranked);
        var teams = new List<PlannedTeam>();

        while (pool.Count >= teamSize)
        {
            var members = new List<Guid>(teamSize);
            for (var i = 0; i < teamSize; i++)
            {
                if (i % 2 == 0)
                {
                    members.Add(pool[0]);
                    pool.RemoveAt(0);
                }
                else
                {
                    members.Add(pool[^1]);
                    pool.RemoveAt(pool.Count - 1);
                }
            }

            teams.Add(new PlannedTeam(members));
        }

        return teams;
    }

    /// <summary>
    /// Splits the ranking in <paramref name="teamSize"/> equally big slices and takes the n'th player
    /// of every slice: with 10 players and teams of two that is 1+6, 2+7, 3+8, 4+9, 5+10.
    /// </summary>
    private static List<PlannedTeam> BuildFairTeams(List<Guid> ranked, int teamSize)
    {
        var teamCount = ranked.Count / teamSize;
        if (teamCount == 0)
        {
            return [];
        }

        var slices = Enumerable.Range(0, teamSize)
            .Select(slice => ranked.Skip(slice * teamCount).Take(teamCount).ToList())
            .ToList();

        return Enumerable.Range(0, teamCount)
            .Select(index => new PlannedTeam(slices.Select(slice => slice[index]).ToList()))
            .ToList();
    }

    private static List<T> Rotate<T>(IReadOnlyList<T> items, int offset)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var shift = ((offset % items.Count) + items.Count) % items.Count;
        return items.Skip(shift).Concat(items.Take(shift)).ToList();
    }

    private static List<Guid> Shuffle(IReadOnlyList<Guid> items, Random random)
    {
        var shuffled = new List<Guid>(items);
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }
}
