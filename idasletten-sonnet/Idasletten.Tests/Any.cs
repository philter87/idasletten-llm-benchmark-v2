using Bogus;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;

namespace Idasletten.Tests;

/// <summary>
/// Static factory for test data with random values.
/// </summary>
public static class Any
{
    private static readonly Faker Faker = new("en");

    public static User User(Action<User>? configure = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = Faker.Random.AlphaNumeric(3).ToUpper(),
            Name = Faker.Name.FullName(),
            Email = Faker.Internet.Email(),
        };
        configure?.Invoke(user);
        return user;
    }

    public static Tournament Tournament(Action<Tournament>? configure = null)
    {
        var t = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = Faker.Commerce.ProductName(),
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
        };
        configure?.Invoke(t);
        return t;
    }

    public static TournamentPlayer TournamentPlayer(Guid tournamentId, Guid userId, Action<TournamentPlayer>? configure = null)
    {
        var p = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            UserId = userId,
            Score = 1000,
            Lives = 3,
        };
        configure?.Invoke(p);
        return p;
    }

    public static TournamentMatch TournamentMatch(Guid tournamentId, Action<TournamentMatch>? configure = null)
    {
        var m = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Order = Faker.Random.Int(1, 100),
            State = MatchState.Planned,
        };
        configure?.Invoke(m);
        return m;
    }
}
