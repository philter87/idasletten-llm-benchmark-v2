namespace Idasletten.Tests.Any;

public static class Any
{
    private static readonly Random Random = new();

    public static string String(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    public static int Int(int min = 0, int max = 100) => Random.Next(min, max);
    public static double Double(double min = 0, double max = 1000) => min + Random.NextDouble() * (max - min);
    public static bool Bool() => Random.Next(2) == 0;
    public static Guid Guid() => System.Guid.NewGuid();

    public static string Initials() => new string(new[]
    {
        (char)('A' + Random.Next(26)),
        (char)('A' + Random.Next(26)),
        (char)('A' + Random.Next(26))
    });

    public static Features.Users.AppUser User() => new()
    {
        Id = Guid(),
        UserName = $"{String(5)}@example.com",
        Email = $"{String(5)}@example.com",
        Username = Initials(),
        Name = $"User {String(5)}",
        EmailConfirmed = true
    };

    public static Features.Tournaments.Tournament Tournament() => new()
    {
        Id = Guid(),
        Name = $"Tournament {String(8)}",
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = Features.Tournaments.ScoreSystem.Elo,
        IsPublic = Bool(),
        IsArchived = false,
        RoundNumber = 1
    };

    public static Features.Players.TournamentPlayer TournamentPlayer(Guid tournamentId, Guid userId) => new()
    {
        Id = Guid(),
        TournamentId = tournamentId,
        UserId = userId,
        Score = 1500,
        Lives = 3,
        TrueSkillMean = 25,
        TrueSkillStdDev = 25.0 / 3.0
    };
}
