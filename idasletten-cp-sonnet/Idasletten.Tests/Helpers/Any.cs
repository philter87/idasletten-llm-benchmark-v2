using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Users.Entities;

namespace Idasletten.Tests.Helpers;

/// <summary>
/// Test data factory — creates entities initialized with random values.
/// </summary>
public static class Any
{
    private static readonly Random Rng = new Random(42);

    public static string String(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Rng.Next(chars.Length)]).ToArray());
    }

    public static User User(string? username = null, string? name = null, string? email = null)
    {
        var initials = username ?? String(3);
        return new User
        {
            Id = Guid.NewGuid(),
            Username = initials,
            Name = name ?? $"Test User {initials}",
            Email = email,
        };
    }

    public static Tournament Tournament(
        string? name = null,
        ScoreSystem scoreSystem = ScoreSystem.Elo,
        int teamSize = 2,
        int pointsToWin = 5,
        bool isPublic = true)
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Tournament {String(4)}",
            TeamSize = teamSize,
            PointsToWin = pointsToWin,
            ScoreSystem = scoreSystem,
            IsPublic = isPublic,
        };
    }

    public static TournamentPlayer TournamentPlayer(
        Guid? userId = null,
        Guid? tournamentId = null,
        double score = 1000.0,
        int lives = 3)
    {
        return new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            TournamentId = tournamentId ?? Guid.NewGuid(),
            Score = score,
            Lives = lives,
        };
    }
}
