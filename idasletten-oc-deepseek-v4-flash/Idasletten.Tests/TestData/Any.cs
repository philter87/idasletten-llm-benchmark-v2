using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;

namespace Idasletten.Tests.TestData;

public static class Any
{
    private static readonly Random _random = new();

    public static User User()
    {
        var initials = GenerateInitials();
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = initials,
            Initials = initials,
            Name = $"Test User {initials}",
            Email = $"{initials.ToLowerInvariant()}@test.com"
        };
    }

    public static Tournament Tournament()
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Name = $"Tournament {_random.Next(1000)}",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true
        };
    }

    public static TournamentPlayer TournamentPlayer(Guid userId, Guid tournamentId)
    {
        return new TournamentPlayer
        {
            UserId = userId,
            TournamentId = tournamentId,
            Score = 1000,
            Lives = 3
        };
    }

    private static string GenerateInitials()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 3).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }
}
