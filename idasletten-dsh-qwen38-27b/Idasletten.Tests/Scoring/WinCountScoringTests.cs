using Idasletten.Models;
using Idasletten.Scoring;

namespace Idasletten.Tests.Scoring;

public class WinCountScoringTests
{
    private readonly WinCountScoring _engine = new();

    [Fact]
    public void Should_MirrorWinCountAsScore_When_PlayerWins()
    {
        // Arrange — facade increments WinCount before calling Apply
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        a.WinCount = 3;
        var all = TwoTeams(a, 5, b, 2);

        // Act
        _engine.Apply([a], 5, all);

        // Assert
        Assert.Equal(3, a.Score);
        Assert.Equal(1, a.ScoreDiff);
    }

    [Fact]
    public void Should_KeepScoreAtWinCount_When_PlayerLoses()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        a.WinCount = 1;
        var all = TwoTeams(a, 2, b, 5);

        // Act
        _engine.Apply([a], 2, all);

        // Assert
        Assert.Equal(1, a.Score);
        Assert.Equal(0, a.ScoreDiff);
    }

    [Fact]
    public void Should_StartAtZero_When_PlayerIsInitialized()
    {
        // Arrange
        var p = Any.Player();
        p.Score = 7;

        // Act
        _engine.Initialize(p);

        // Assert
        Assert.Equal(0, p.Score);
    }

    private static List<TeamResult> TwoTeams(TournamentPlayer a, int goalsA, TournamentPlayer b, int goalsB)
    {
        var teamA = new TeamResult { Players = [a], Goals = goalsA };
        var teamB = new TeamResult { Players = [b], Goals = goalsB };
        return [teamA, teamB];
    }
}
