using Idasletten.Features.Matches;
using Idasletten.Features.TournamentPlayers;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;

namespace Idasletten.Tests.TestData;

/// Random valid test data factories, per slice naming convention: Any.User(), Any.Tournament(), etc.
public static class Any
{
    private static readonly Random Random = new();

    public static string Username() =>
        new(Enumerable.Range(0, 3).Select(_ => (char)Random.Next('A', 'Z' + 1)).ToArray());

    public static string Word(int length = 8) =>
        new(Enumerable.Range(0, length).Select(_ => (char)Random.Next('a', 'z' + 1)).ToArray());

    public static int Int(int min = 1, int max = 1000) => Random.Next(min, max);

    public static User User()
    {
        var username = Username() + Int(1, 9999);
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Name = $"{Word()} {Word()}",
            Email = $"{Word()}@example.com",
            NormalizedEmail = $"{Word()}@example.com".ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    public static Tournament Tournament(ScoreSystem? scoreSystem = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{Word()} turnering",
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = scoreSystem ?? ScoreSystem.Elo,
        IsPublic = true,
        IsArchived = false,
        CreatedAtUtc = DateTime.UtcNow
    };

    public static TournamentPlayer TournamentPlayer(Guid tournamentId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournamentId,
        UserId = userId,
        Score = Int(0, 2000),
        WinCount = Int(0, 20),
        LoseCount = Int(0, 20),
        MatchCount = Int(0, 40),
        Lives = Int(0, 3)
    };

    public static TournamentMatch Match(Guid tournamentId, MatchState state = MatchState.Planned) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournamentId,
        Order = Int(),
        State = state
    };
}
