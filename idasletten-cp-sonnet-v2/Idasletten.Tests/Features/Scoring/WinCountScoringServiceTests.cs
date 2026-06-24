namespace Idasletten.Tests.Features.Scoring;

public class WinCountScoringServiceTests
{
    private readonly Idasletten.Features.Scoring.WinCountScoringService _service = new();

    [Fact]
    public void Should_IncrementWinCount_When_TeamWins()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: Idasletten.Shared.Entities.ScoreSystem.WinCount);
        var winner = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        var loser = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        winner.WinCount = 2;
        winner.Score = 2;
        loser.WinCount = 1;
        loser.LoseCount = 0;

        // Act
        _service.CalculateScores([winner], [loser], 5, 1, tournament);

        // Assert
        Assert.Equal(3, winner.WinCount);
        Assert.Equal(3.0, winner.Score);
        Assert.Equal(1, loser.LoseCount);
    }
}
