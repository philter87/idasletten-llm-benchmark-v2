using Idasletten.Shared.Data;

namespace Idasletten.Tests;

public static class Any
{
    public static AppUser User(string? initials = null) => new()
    {
        UserName = initials ?? RandomInitials(),
        NormalizedUserName = initials ?? RandomInitials(),
        Name = $"Player {Guid.NewGuid():N}"[..18],
        Email = $"{Guid.NewGuid():N}@example.test",
        ImageUrl = null
    };

    public static Tournament Tournament(string? name = null) => new()
    {
        Name = name ?? $"Tournament {Guid.NewGuid():N}"[..24],
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = ScoreSystem.Elo,
        IsPublic = true
    };

    private static string RandomInitials() => Guid.NewGuid().ToString("N")[..3].ToUpperInvariant();
}
