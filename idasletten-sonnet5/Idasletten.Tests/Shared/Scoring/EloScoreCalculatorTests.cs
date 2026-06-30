using Idasletten.Features.TournamentPlayers;
using Idasletten.Shared.Scoring;

namespace Idasletten.Tests.Shared.Scoring;

public class EloScoreCalculatorTests
{
    [Fact]
    public void Should_SetStartingRating_When_PlayerIsReset()
    {
        // Arrange
        var calculator = new EloScoreCalculator();
        var player = new TournamentPlayer { Id = Guid.NewGuid() };

        // Act
        calculator.ResetPlayer(player);

        // Assert
        Assert.Equal(1200, player.Score);
    }

    [Fact]
    public void Should_IncreaseWinnerScoreAndDecreaseLoserScore_When_EqualRatingTeamsPlay()
    {
        // Arrange
        var calculator = new EloScoreCalculator();
        var winner = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1200 };
        var loser = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1200 };
        var teams = new[]
        {
            new TeamOutcome(Guid.NewGuid(), 5, 2, [winner]),
            new TeamOutcome(Guid.NewGuid(), 2, 5, [loser])
        };

        // Act
        calculator.ApplyMatch(teams);

        // Assert
        Assert.True(winner.Score > 1200);
        Assert.True(loser.Score < 1200);
        Assert.Equal(2400, winner.Score + loser.Score, precision: 6);
    }

    [Fact]
    public void Should_AverageTeamRatings_When_TeamHasMultiplePlayers()
    {
        // Arrange
        var calculator = new EloScoreCalculator();
        var strongPlayer = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1600 };
        var weakPlayer = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1000 };
        var opponent = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1300 };
        var teams = new[]
        {
            new TeamOutcome(Guid.NewGuid(), 5, 1, [strongPlayer, weakPlayer]),
            new TeamOutcome(Guid.NewGuid(), 1, 5, [opponent])
        };

        // Act
        calculator.ApplyMatch(teams);

        // Assert: team average (1300) was equal to the opponent's rating, so the win is worth
        // a full K-sized move split evenly between teammates.
        Assert.Equal(strongPlayer.Score - 1600, weakPlayer.Score - 1000, precision: 6);
    }
}
