using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Idasletten.Shared;
using Microsoft.AspNetCore.Identity;

namespace Idasletten.Tests;

public static class Any
{
    private static readonly Random _random = new Random();
    private static readonly string[] _firstNames = new[] { "Jens", "Peter", "Mads", "Lars", "Kristian", "Soren", "Michael", "Thomas", "Anders", "Martin" };
    private static readonly string[] _lastNames = new[] { "Jensen", "Andersen", "Pedersen", "Christensen", "Larsen", "Nielsen", "Hansen", "Madsens", "Kristensen", "Sorensen" };
    private static readonly string[] _tournamentNames = new[] { "Foraarsturnering", "Efteraarssturnering", "Sommerturnering", "Vinterturnering", "Juleturnering", "Paasketurnering" };

    public static User User()
    {
        var firstName = _firstNames[_random.Next(_firstNames.Length)];
        var lastName = _lastNames[_random.Next(_lastNames.Length)];
        var initials = (firstName[0] + lastName[0] + lastName[1]).ToString().ToUpper();
        
        return new User
        {
            UserName = initials,
            Name = firstName + " " + lastName,
            Email = firstName.ToLower() + "." + lastName.ToLower() + "@example.com",
            EmailConfirmed = true,
            Id = Guid.NewGuid()
        };
    }

    public static Tournament Tournament()
    {
        var scoreSystems = Enum.GetValues<ScoreSystem>();
        return new Tournament
        {
            Name = _tournamentNames[_random.Next(_tournamentNames.Length)] + " " + _random.Next(2020, 2025),
            TeamSize = _random.Next(1, 5),
            PointsToWin = new[] { 3, 5, 7, 10 }[_random.Next(4)],
            ScoreSystem = scoreSystems[_random.Next(scoreSystems.Length)],
            MaxPlayerCount = _random.Next(0, 100) > 80 ? _random.Next(2, 21) : null,
            IsArchived = _random.Next(0, 100) < 30,
            IsPublic = _random.Next(0, 100) > 10,
            SeedTournamentId = null,
            ParentTournamentId = null,
            RoundNumber = 1
        };
    }

    public static TournamentPlayer TournamentPlayer(Tournament tournament, User user)
    {
        return new TournamentPlayer
        {
            UserId = user.Id,
            TournamentId = tournament.Id,
            Score = tournament.ScoreSystem switch
            {
                ScoreSystem.Elo => _random.Next(1000, 1500),
                ScoreSystem.TrueSkill => Math.Round(_random.NextDouble() * 50, 2),
                ScoreSystem.Lives => _random.Next(0, 10),
                ScoreSystem.WinCount => _random.Next(0, 20),
                _ => 0
            },
            WinCount = _random.Next(0, 10),
            MatchCount = _random.Next(0, 20),
            LoseCount = _random.Next(0, 10),
            Lives = tournament.ScoreSystem == ScoreSystem.Lives ? _random.Next(1, 4) : 3,
            PointsWon = _random.Next(0, 100),
            PointsLost = _random.Next(0, 100),
            ScoreDiff = Math.Round((_random.NextDouble() - 0.5) * 50, 2)
        };
    }

    public static TournamentTeam TournamentTeam(Tournament tournament, int teamNumber = 1)
    {
        return new TournamentTeam
        {
            Name = "Hold " + teamNumber,
            Number = teamNumber,
            TournamentId = tournament.Id
        };
    }

    public static TournamentMatch TournamentMatch(Tournament tournament, int order = 1)
    {
        var states = Enum.GetValues<MatchState>();
        return new TournamentMatch
        {
            Order = order,
            TournamentId = tournament.Id,
            State = states[_random.Next(states.Length)]
        };
    }

    public static TournamentTeamMatchResult TournamentTeamMatchResult(TournamentMatch match, TournamentTeam team)
    {
        return new TournamentTeamMatchResult
        {
            MatchId = match.Id,
            TournamentId = match.TournamentId,
            TeamId = team.Id,
            GoalsWon = _random.Next(0, 11),
            GoalsLost = _random.Next(0, 11)
        };
    }

    public static IdentityRole<Guid> Role()
    {
        var roleNames = new[] { "Admin", "User", "Moderator", "TournamentCreator" };
        return new IdentityRole<Guid>
        {
            Name = roleNames[_random.Next(roleNames.Length)],
            NormalizedName = roleNames[_random.Next(roleNames.Length)].ToUpper()
        };
    }

    public static string Initials()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var initials = new char[3];
        for (int i = 0; i < 3; i++)
        {
            initials[i] = chars[_random.Next(chars.Length)];
        }
        return new string(initials);
    }

    public static int Score()
    {
        return _random.Next(0, 3000);
    }

    public static double ScoreDouble()
    {
        return Math.Round(_random.NextDouble() * 100, 2);
    }
}

public static class TestConstants
{
    public static readonly Guid TestTournamentId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    public static readonly Guid TestUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
    public static readonly string TestUserEmail = "test@idasletten.local";
    public static readonly string TestUserPassword = "Test123!";
    public static readonly string TestUserName = "TST";
}

public static class TestUserFactory
{
    public static User Create()
    {
        return new User
        {
            Id = TestConstants.TestUserId,
            UserName = TestConstants.TestUserName,
            Name = "Test User",
            Email = TestConstants.TestUserEmail,
            EmailConfirmed = true
        };
    }
}

public static class TestTournamentFactory
{
    public static Tournament Create()
    {
        return new Tournament
        {
            Id = TestConstants.TestTournamentId,
            Name = "Test Tournament",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            IsArchived = false
        };
    }
}
