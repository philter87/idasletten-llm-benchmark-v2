using Idasletten.Features.TournamentPlayers;
using Idasletten.Shared.Scoring;

namespace Idasletten.Tests.Shared.Scoring;

public class WinCountScoreCalculatorTests
{
    [Fact]
    public void Should_SetScoreToZero_When_PlayerIsReset()
    {
        // Arrange
        var calculator = new WinCountScoreCalculator();
        var player = new TournamentPlayer { Id = Guid.NewGuid(), Score = 7 };

        // Act
        calculator.ResetPlayer(player);

        // Assert
        Assert.Equal(0, player.Score);
    }

    [Fact]
    public void Should_IncrementOnlyWinnerScore_When_MatchIsPlayed()
    {
        // Arrange
        var calculator = new WinCountScoreCalculator();
        var winner = new TournamentPlayer { Id = Guid.NewGuid(), Score = 2 };
        var loser = new TournamentPlayer { Id = Guid.NewGuid(), Score = 2 };
        var teams = new[]
        {
            new TeamOutcome(Guid.NewGuid(), 5, 2, [winner]),
            new TeamOutcome(Guid.NewGuid(), 2, 5, [loser])
        };

        // Act
        calculator.ApplyMatch(teams);

        // Assert
        Assert.Equal(3, winner.Score);
        Assert.Equal(2, loser.Score);
    }
}
