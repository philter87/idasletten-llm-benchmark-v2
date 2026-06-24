using Idasletten.Features.Scoring;
using Idasletten.Shared.Entities;

namespace Idasletten.Tests.Features.Scoring;

public class LivesScoringServiceTests
{
    private readonly LivesScoringService _service = new();

    [Fact]
    public void Should_LoseALife_When_TeamLoses()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Lives);
        var winner = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        var loser = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        winner.Lives = 3;
        winner.WinCount = 0;
        loser.Lives = 3;
        loser.LoseCount = 0;

        // Act
        _service.CalculateScores([winner], [loser], 5, 2, tournament);

        // Assert
        Assert.Equal(3, winner.Lives);
        Assert.Equal(2, loser.Lives);
        Assert.Equal(1, winner.WinCount);
        Assert.Equal(1, loser.LoseCount);
    }

    [Fact]
    public void Should_NotGoBelowZeroLives_When_AlreadyAtZero()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Lives);
        var winner = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        var loser = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        loser.Lives = 0;

        // Act
        _service.CalculateScores([winner], [loser], 5, 0, tournament);

        // Assert
        Assert.Equal(0, loser.Lives);
    }
}
