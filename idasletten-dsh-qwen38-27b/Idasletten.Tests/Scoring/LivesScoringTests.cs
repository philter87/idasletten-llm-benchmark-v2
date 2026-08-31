using Idasletten.Models;
using Idasletten.Scoring;

namespace Idasletten.Tests.Scoring;

public class LivesScoringTests
{
    private readonly LivesScoring _engine = new();

    [Fact]
    public void Should_LoseOneLife_When_PlayerLoses()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        var all = TwoTeams(a, 2, b, 5);

        // Act
        _engine.Apply([a], 2, all);
        _engine.Apply([b], 5, all);

        // Assert
        Assert.Equal(2, a.Lives);
        Assert.Equal(2, a.Score);
        Assert.Equal(LivesScoring.StartingLives, b.Lives);
    }

    [Fact]
    public void Should_FloorAtZeroLives_When_PlayerLosesLastLife()
    {
        // Arrange
        var p = Any.Player();
        _engine.Initialize(p);
        p.Lives = 1; p.Score = 1;
        var all = TwoTeams(p, 0, Any.Player(), 5);

        // Act
        _engine.Apply([p], 0, all);

        // Assert
        Assert.Equal(0, p.Lives);
        Assert.Equal(0, p.Score);
    }

    [Fact]
    public void Should_KeepLives_When_PlayerWins()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        a.Lives = 2; a.Score = 2;
        var all = TwoTeams(a, 5, b, 3);

        // Act
        _engine.Apply([a], 5, all);

        // Assert
        Assert.Equal(2, a.Lives);
    }

    [Fact]
    public void Should_StartWithThreeLives_When_PlayerIsInitialized()
    {
        // Arrange
        var p = Any.Player();

        // Act
        _engine.Initialize(p);

        // Assert
        Assert.Equal(3, p.Lives);
        Assert.Equal(3, p.Score);
    }

    private static List<TeamResult> TwoTeams(TournamentPlayer a, int goalsA, TournamentPlayer b, int goalsB)
    {
        var teamA = new TeamResult { Players = [a], Goals = goalsA };
        var teamB = new TeamResult { Players = [b], Goals = goalsB };
        return [teamA, teamB];
    }
}
