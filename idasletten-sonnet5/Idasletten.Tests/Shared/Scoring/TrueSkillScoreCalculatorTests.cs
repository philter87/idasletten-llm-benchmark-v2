using Idasletten.Features.TournamentPlayers;
using Idasletten.Shared.Scoring;

namespace Idasletten.Tests.Shared.Scoring;

public class TrueSkillScoreCalculatorTests
{
    [Fact]
    public void Should_RaiseWinnerScoreAboveLoserScore_When_EvenlyMatchedPlayersPlay()
    {
        // Arrange
        var calculator = new TrueSkillScoreCalculator();
        var winner = new TournamentPlayer { Id = Guid.NewGuid() };
        var loser = new TournamentPlayer { Id = Guid.NewGuid() };
        calculator.ResetPlayer(winner);
        calculator.ResetPlayer(loser);
        var startingScore = winner.Score;
        var teams = new[]
        {
            new TeamOutcome(Guid.NewGuid(), 5, 2, [winner]),
            new TeamOutcome(Guid.NewGuid(), 2, 5, [loser])
        };

        // Act
        calculator.ApplyMatch(teams);

        // Assert
        Assert.True(winner.Score > startingScore);
        Assert.True(loser.Score < startingScore);
    }

    [Fact]
    public void Should_KeepWorkingRatingAcrossMatches_When_SamePlayerPlaysTwice()
    {
        // Arrange: a calculator instance is meant to live for one full chronological replay,
        // so its internal rating state must carry over between ApplyMatch calls.
        var calculator = new TrueSkillScoreCalculator();
        var player = new TournamentPlayer { Id = Guid.NewGuid() };
        var opponent1 = new TournamentPlayer { Id = Guid.NewGuid() };
        var opponent2 = new TournamentPlayer { Id = Guid.NewGuid() };
        calculator.ResetPlayer(player);
        calculator.ResetPlayer(opponent1);
        calculator.ResetPlayer(opponent2);

        // Act
        calculator.ApplyMatch([
            new TeamOutcome(Guid.NewGuid(), 5, 0, [player]),
            new TeamOutcome(Guid.NewGuid(), 0, 5, [opponent1])
        ]);
        var scoreAfterFirstWin = player.Score;
        calculator.ApplyMatch([
            new TeamOutcome(Guid.NewGuid(), 5, 0, [player]),
            new TeamOutcome(Guid.NewGuid(), 0, 5, [opponent2])
        ]);

        // Assert: a second win should push the rating up further still, not reset it.
        Assert.True(player.Score > scoreAfterFirstWin);
    }
}
