using Idasletten.Features.Scoring;
using Idasletten.Shared.Enums;

namespace Idasletten.Tests.Features.Scoring;

public class LivesScoreCalculatorTests
{
    private readonly LivesScoreCalculator _calculator = new();

    [Fact]
    public void Should_RemoveLife_When_PlayerLoses()
    {
        // Arrange
        var tournament = Any.Tournament(t => t.ScoreSystem = ScoreSystem.Lives);
        var loser = Any.TournamentPlayer(tournament.Id, Guid.NewGuid(), p => p.Lives = 3);
        var winner = Any.TournamentPlayer(tournament.Id, Guid.NewGuid(), p => p.Lives = 3);

        // Act
        _calculator.UpdateScores([winner], [loser], 5, 2, tournament);

        // Assert
        Assert.Equal(2, loser.Lives);
        Assert.Equal(3, winner.Lives);
    }

    [Fact]
    public void Should_NotGoBelowZeroLives_When_PlayerAlreadyAtZero()
    {
        // Arrange
        var tournament = Any.Tournament(t => t.ScoreSystem = ScoreSystem.Lives);
        var loser = Any.TournamentPlayer(tournament.Id, Guid.NewGuid(), p => p.Lives = 0);
        var winner = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());

        // Act
        _calculator.UpdateScores([winner], [loser], 5, 2, tournament);

        // Assert
        Assert.Equal(0, loser.Lives);
    }
}
