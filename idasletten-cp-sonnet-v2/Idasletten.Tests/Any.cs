using Idasletten.Shared.Entities;

namespace Idasletten.Tests;

/// <summary>
/// Test data factory — creates entities with all fields set to random/sensible values.
/// </summary>
public static class Any
{
    private static readonly Random Rng = new();

    public static User User(string? username = null) => new()
    {
        Id = Guid.NewGuid(),
        Username = username ?? RandomInitials(),
        Name = $"Test {RandomInitials()}",
        Email = $"{RandomInitials().ToLower()}@test.com",
        CreatedAt = DateTime.UtcNow
    };

    public static Tournament Tournament(
        string? name = null,
        ScoreSystem scoreSystem = ScoreSystem.Elo,
        bool isPublic = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name ?? $"Tournament {Guid.NewGuid().ToString()[..8]}",
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = scoreSystem,
        IsPublic = isPublic,
        IsArchived = false,
        CreatedAt = DateTime.UtcNow
    };

    public static TournamentPlayer TournamentPlayer(Guid userId, Guid tournamentId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TournamentId = tournamentId,
        Score = 1000 + Rng.NextDouble() * 200 - 100,
        WinCount = Rng.Next(0, 10),
        LoseCount = Rng.Next(0, 10),
        MatchCount = Rng.Next(0, 20),
        Lives = 3,
        PointsWon = Rng.Next(0, 50),
        PointsLost = Rng.Next(0, 50),
        ScoreDiff = 0
    };

    private static string RandomInitials()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 3).Select(_ => chars[Rng.Next(chars.Length)]).ToArray());
    }
}
