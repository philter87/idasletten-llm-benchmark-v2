using Idasletten.Shared.Domain;

namespace Idasletten.Tests;

/// <summary>
/// Test-data factory. Every method initialises all fields with random values so a test only has
/// to pin down the fields it actually cares about.
/// </summary>
public static class Any
{
    private static readonly Random Rng = new();

    public static string String(string prefix = "x") => $"{prefix}-{Guid.NewGuid():N}".Substring(0, 12);
    public static int Int(int min = 1, int max = 100) => Rng.Next(min, max);
    public static bool Bool() => Rng.Next(2) == 0;
    public static T Enum<T>() where T : struct, System.Enum
    {
        var values = System.Enum.GetValues<T>();
        return values[Rng.Next(values.Length)];
    }

    public static string Initials()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 3).Select(_ => letters[Rng.Next(letters.Length)]).ToArray());
    }

    public static User User()
    {
        var initials = Initials();
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = initials,
            NormalizedUserName = initials.ToUpperInvariant(),
            Name = String("name"),
            Email = $"{initials.ToLowerInvariant()}@example.com",
            NormalizedEmail = $"{initials.ToUpperInvariant()}@EXAMPLE.COM",
            ImageUrl = null
        };
    }

    public static Tournament Tournament(ScoreSystem? scoreSystem = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = String("cup"),
        TeamSize = 2,
        PointsToWin = Int(3, 11),
        ScoreSystem = scoreSystem ?? Enum<ScoreSystem>(),
        MaxPlayerCount = Bool() ? Int(4, 16) : null,
        IsPublic = Bool(),
        IsArchived = false
    };
}
