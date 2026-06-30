using Idasletten.Features.TournamentPlayers;
using Idasletten.Shared.Scoring;

namespace Idasletten.Tests.Shared.Scoring;

public class LivesScoreCalculatorTests
{
    [Fact]
    public void Should_SetThreeLives_When_PlayerIsReset()
    {
        // Arrange
        var calculator = new LivesScoreCalculator();
        var player = new TournamentPlayer { Id = Guid.NewGuid() };

        // Act
        calculator.ResetPlayer(player);

        // Assert
        Assert.Equal(3, player.Lives);
        Assert.Equal(3, player.Score);
    }

    [Fact]
    public void Should_DecrementLoserLives_When_TeamLoses()
    {
        // Arrange
        var calculator = new LivesScoreCalculator();
        var winner = new TournamentPlayer { Id = Guid.NewGuid(), Lives = 3 };
        var loser = new TournamentPlayer { Id = Guid.NewGuid(), Lives = 3 };
        var teams = new[]
        {
            new TeamOutcome(Guid.NewGuid(), 5, 2, [winner]),
            new TeamOutcome(Guid.NewGuid(), 2, 5, [loser])
        };

        // Act
        calculator.ApplyMatch(teams);

        // Assert
        Assert.Equal(3, winner.Lives);
        Assert.Equal(2, loser.Lives);
        Assert.Equal(2, loser.Score);
    }

    [Fact]
    public void Should_NotGoBelowZero_When_PlayerHasNoLivesLeft()
    {
        // Arrange
        var calculator = new LivesScoreCalculator();
        var winner = new TournamentPlayer { Id = Guid.NewGuid(), Lives = 3 };
        var loser = new TournamentPlayer { Id = Guid.NewGuid(), Lives = 0 };
        var teams = new[]
        {
            new TeamOutcome(Guid.NewGuid(), 5, 2, [winner]),
            new TeamOutcome(Guid.NewGuid(), 2, 5, [loser])
        };

        // Act
        calculator.ApplyMatch(teams);

        // Assert
        Assert.Equal(0, loser.Lives);
    }
}
