using Idasletten.Shared.Entities;

namespace Idasletten.Tests.TestSupport;

/// <summary>Test data factory: every method initializes all fields with random values, so tests only need to override what they care about.</summary>
public static class Any
{
    private static int _counter;

    public static string String(int length = 8)
    {
        var n = Interlocked.Increment(ref _counter);
        return $"{Guid.NewGuid():N}{n}"[..length];
    }

    public static string Initials() => string.Concat(Guid.NewGuid().ToString("N").Take(3)).ToUpperInvariant();

    public static int Int(int min = 1, int max = 100) => Random.Shared.Next(min, max);

    public static User User()
    {
        var username = Initials();
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Name = $"Player {String(6)}",
            Email = $"{String(8)}@example.com".ToLowerInvariant(),
        };
    }

    public static Tournament Tournament(
        ScoreSystem scoreSystem = ScoreSystem.Elo,
        int teamSize = 2,
        int? maxPlayerCount = null,
        bool isPublic = true,
        bool isArchived = false,
        Guid? parentTournamentId = null,
        Guid? seedTournamentId = null)
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Name = $"Tournament {String(8)}",
            TeamSize = teamSize,
            PointsToWin = Int(3, 10),
            ScoreSystem = scoreSystem,
            MaxPlayerCount = maxPlayerCount,
            IsPublic = isPublic,
            IsArchived = isArchived,
            ParentTournamentId = parentTournamentId,
            SeedTournamentId = seedTournamentId,
        };
    }

    public static TournamentPlayer TournamentPlayer(Guid tournamentId, Guid userId, double score = 1000, int lives = 3)
    {
        return new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            UserId = userId,
            Score = score,
            Lives = lives,
        };
    }

    public static TournamentMatch TournamentMatch(Guid tournamentId, int order = 1, MatchState state = MatchState.Planned)
    {
        return new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Order = order,
            State = state,
        };
    }
}
