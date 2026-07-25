using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;

namespace Idasletten.Tests;

/// <summary>
/// Test data factory. Every method fills in all fields with random values, so a test only has to
/// mention the values it actually cares about.
/// </summary>
public static class Any
{
    private static readonly Random Random = new(20260724);

    private static readonly string[] Names =
    [
        "Thor", "Odin", "Loke", "Frøya", "Tyr", "Balder", "Sif", "Heimdal", "Brage", "Iduna",
    ];

    public static int Int(int min = 1, int max = 100)
    {
        lock (Random)
        {
            return Random.Next(min, max);
        }
    }

    public static double Double(double min = 0, double max = 100) => min + (Int(0, 10_000) / 10_000.0 * (max - min));

    public static bool Bool() => Int(0, 2) == 1;

    public static string Letters(int count)
    {
        lock (Random)
        {
            return new string(Enumerable.Range(0, count)
                .Select(_ => (char)('A' + Random.Next(0, 26)))
                .ToArray());
        }
    }

    public static string Initials() => Letters(3);

    public static string Name() => $"{Names[Int(0, Names.Length)]} {Letters(6)}son";

    public static string Email() => $"{Letters(5).ToLowerInvariant()}@mjolner.dk";

    public static ScoreSystem ScoreSystem() => (ScoreSystem)Int(0, 4);

    public static User User(string? initials = null, string? name = null) => new()
    {
        Id = Guid.NewGuid(),
        UserName = initials ?? Initials(),
        NormalizedUserName = (initials ?? Initials()).ToUpperInvariant(),
        Name = name ?? Name(),
        Email = Email(),
        NormalizedEmail = Email().ToUpperInvariant(),
        ImageUrl = null,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        CreatedUtc = DateTime.UtcNow.AddDays(-Int(1, 100)),
    };

    public static Tournament Tournament(
        string? name = null,
        ScoreSystem? scoreSystem = null,
        int? teamSize = null,
        int? pointsToWin = null,
        int? maxPlayerCount = null,
        bool? isPublic = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name ?? $"Turnering {Letters(4)}",
        TeamSize = teamSize ?? 2,
        PointsToWin = pointsToWin ?? Int(5, 11),
        ScoreSystem = scoreSystem ?? Features.Tournaments.ScoreSystem.Elo,
        MaxPlayerCount = maxPlayerCount,
        IsArchived = false,
        IsPublic = isPublic ?? true,
        RoundNumber = 1,
        CreatedUtc = DateTime.UtcNow.AddDays(-Int(1, 30)),
    };

    public static TournamentPlayer Player(
        Guid? tournamentId = null, Guid? userId = null, double? score = null) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournamentId ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        Score = score ?? Double(1000, 1400),
        WinCount = Int(0, 10),
        LoseCount = Int(0, 10),
        MatchCount = Int(0, 20),
        Lives = Int(0, 4),
        PointsWon = Int(0, 60),
        PointsLost = Int(0, 60),
        ScoreDiff = Double(-30, 30),
        SkillMean = 25,
        SkillDeviation = 25.0 / 3.0,
        CreatedUtc = DateTime.UtcNow,
    };

    public static TournamentTeam Team(Guid tournamentId, int number, params Guid[] playerIds) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournamentId,
        Number = number,
        Name = $"Team {number}",
        Players = playerIds.Select(id => new TournamentTeamPlayer { TournamentPlayerId = id }).ToList(),
    };

    public static TournamentMatch Match(Guid tournamentId, int order = 1, MatchState? state = null) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournamentId,
        Order = order,
        State = state ?? MatchState.Planned,
        CreatedUtc = DateTime.UtcNow,
    };
}
