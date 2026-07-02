using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;

namespace Idasletten.Tests;

/// <summary>Test data factory. Every field gets a random value unless overridden.</summary>
public static class Any
{
    private static readonly Random Random = new();

    public static string Initials() =>
        new string(Enumerable.Range(0, 3).Select(_ => (char)Random.Next('A', 'Z' + 1)).ToArray())
        + Random.Next(10, 99);

    public static string Name() => $"Kriger {Guid.NewGuid():N}"[..20];

    public static string TournamentName() => $"Turnering {Guid.NewGuid():N}"[..24];

    public static string Email() => $"{Guid.NewGuid():N}@idasletten.local";

    public static int Int(int min = 1, int max = 100) => Random.Next(min, max);

    public static User User() => new()
    {
        Id = Guid.NewGuid(),
        UserName = Initials(),
        NormalizedUserName = Initials().ToUpperInvariant(),
        Name = Name(),
        Email = Email(),
        ImageUrl = $"https://img.idasletten.local/{Guid.NewGuid():N}.jpg"
    };

    public static Tournament Tournament(ScoreSystem? scoreSystem = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = TournamentName(),
        TeamSize = Random.Next(1, 3),
        PointsToWin = Random.Next(3, 11),
        ScoreSystem = scoreSystem ?? ScoreSystem.Elo,
        IsPublic = Random.Next(2) == 0
    };
}
