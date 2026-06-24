using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;

namespace Idasletten.Tests;

public static class Any
{
    private static readonly Random _random = new Random();
    private static int _counter = 0;
    
    public static string String(int maxLength = 50)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var length = _random.Next(1, maxLength + 1);
        return new string(Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    public static string Initials()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var length = _random.Next(2, 4);
        return new string(Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    public static string Name()
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emily" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia" };
        return $"{firstNames[_random.Next(firstNames.Length)]} {lastNames[_random.Next(lastNames.Length)]}";
    }
    
    public static User User() => new()
    {
        Id = System.Guid.NewGuid().ToString(),
        UserName = Initials(),
        NormalizedUserName = Initials().ToUpper(),
        Email = $"{String(10).ToLower()}@test.com",
        NormalizedEmail = $"{String(10).ToLower()}@TEST.COM",
        EmailConfirmed = true,
        Name = Name()
    };
    
    public static Tournament Tournament() => new()
    {
        Id = System.Guid.NewGuid(),
        Name = $"Tournament {++_counter}",
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = ScoreSystem.TrueSkill,
        IsPublic = true
    };
    
    public static TournamentPlayer TournamentPlayer(Tournament t, User u) => new()
    {
        Id = System.Guid.NewGuid(),
        UserId = u.Id,
        TournamentId = t.Id,
        Score = _random.NextDouble() * 2000
    };
}
