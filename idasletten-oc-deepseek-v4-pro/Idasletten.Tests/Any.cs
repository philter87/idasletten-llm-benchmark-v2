using Idasletten.Shared.Entities;

namespace Idasletten.Tests;

public static class Any
{
    private static readonly Random _random = new();

    public static User User(string? username = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username ?? GenerateInitials(),
            Name = $"Test User {_random.Next(1000, 9999)}",
            Email = $"test{_random.Next(1000, 9999)}@example.com",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Tournament Tournament(string? name = null, ScoreSystem? scoreSystem = null, bool? isPublic = null)
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Tournament {_random.Next(1000, 9999)}",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = scoreSystem ?? ScoreSystem.Elo,
            IsPublic = isPublic ?? true,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static TournamentPlayer TournamentPlayer(Guid userId, Guid tournamentId)
    {
        return new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TournamentId = tournamentId,
            Score = 1000 + _random.Next(-200, 200),
            WinCount = _random.Next(0, 10),
            MatchCount = _random.Next(0, 20),
            LoseCount = _random.Next(0, 10),
            Lives = _random.Next(0, 4),
            PointsWon = _random.Next(0, 50),
            PointsLost = _random.Next(0, 50)
        };
    }

    private static string GenerateInitials()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 3).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }
}
