using Idasletten.Shared.Scoring;
using Idasletten.Tests.TestSupport;

namespace Idasletten.Tests.Shared.Scoring;

public class LivesScoreSystemStrategyTests
{
    [Fact]
    public void Should_DecrementLosingTeamLives_When_MatchHasAWinner()
    {
        // Arrange
        var strategy = new LivesScoreSystemStrategy();
        var tournamentId = Guid.NewGuid();
        var winner = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), lives: 3);
        var loser = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), lives: 3);
        var teams = new List<TeamMatchInfo>
        {
            new() { TeamId = Guid.NewGuid(), Players = [winner], GoalsWon = 5, GoalsLost = 1 },
            new() { TeamId = Guid.NewGuid(), Players = [loser], GoalsWon = 1, GoalsLost = 5 },
        };

        // Act
        strategy.ApplyMatch(teams);

        // Assert
        Assert.Equal(3, winner.Lives);
        Assert.Equal(2, loser.Lives);
        Assert.Equal(2, loser.Score);
    }

    [Fact]
    public void Should_NeverGoBelowZeroLives_When_PlayerKeepsLosing()
    {
        // Arrange
        var strategy = new LivesScoreSystemStrategy();
        var tournamentId = Guid.NewGuid();
        var loser = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), lives: 0);
        var teams = new List<TeamMatchInfo>
        {
            new() { TeamId = Guid.NewGuid(), Players = [Any.TournamentPlayer(tournamentId, Guid.NewGuid())], GoalsWon = 5, GoalsLost = 0 },
            new() { TeamId = Guid.NewGuid(), Players = [loser], GoalsWon = 0, GoalsLost = 5 },
        };

        // Act
        strategy.ApplyMatch(teams);

        // Assert
        Assert.Equal(0, loser.Lives);
    }

    [Fact]
    public void Should_NotChangeLives_When_MatchIsADraw()
    {
        // Arrange
        var strategy = new LivesScoreSystemStrategy();
        var tournamentId = Guid.NewGuid();
        var playerA = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), lives: 3);
        var playerB = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), lives: 3);
        var teams = new List<TeamMatchInfo>
        {
            new() { TeamId = Guid.NewGuid(), Players = [playerA], GoalsWon = 3, GoalsLost = 3 },
            new() { TeamId = Guid.NewGuid(), Players = [playerB], GoalsWon = 3, GoalsLost = 3 },
        };

        // Act
        strategy.ApplyMatch(teams);

        // Assert
        Assert.Equal(3, playerA.Lives);
        Assert.Equal(3, playerB.Lives);
    }
}
