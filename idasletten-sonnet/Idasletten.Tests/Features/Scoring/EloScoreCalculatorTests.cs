using Idasletten.Features.Scoring;
using Idasletten.Shared.Enums;

namespace Idasletten.Tests.Features.Scoring;

public class EloScoreCalculatorTests
{
    private readonly EloScoreCalculator _calculator = new();

    [Fact]
    public void Should_IncreaseWinnerScore_When_MatchIsPlayed()
    {
        // Arrange
        var tournament = Any.Tournament(t => t.ScoreSystem = ScoreSystem.Elo);
        var winner = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());
        var loser = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());

        double originalWinnerScore = winner.Score;
        double originalLoserScore = loser.Score;

        // Act
        _calculator.UpdateScores([winner], [loser], 5, 3, tournament);

        // Assert
        Assert.True(winner.Score > originalWinnerScore);
        Assert.True(loser.Score < originalLoserScore);
    }

    [Fact]
    public void Should_TrackWinAndLossCount_When_MatchIsPlayed()
    {
        // Arrange
        var tournament = Any.Tournament();
        var player1 = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());
        var player2 = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());

        // Act
        _calculator.UpdateScores([player1], [player2], 5, 2, tournament);

        // Assert
        Assert.Equal(1, player1.WinCount);
        Assert.Equal(0, player1.LoseCount);
        Assert.Equal(1, player2.LoseCount);
        Assert.Equal(0, player2.WinCount);
    }

    [Fact]
    public void Should_TrackGoals_When_MatchIsPlayed()
    {
        // Arrange
        var tournament = Any.Tournament();
        var player1 = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());
        var player2 = Any.TournamentPlayer(tournament.Id, Guid.NewGuid());

        // Act
        _calculator.UpdateScores([player1], [player2], 5, 3, tournament);

        // Assert
        Assert.Equal(5, player1.PointsWon);
        Assert.Equal(3, player1.PointsLost);
        Assert.Equal(3, player2.PointsWon);
        Assert.Equal(5, player2.PointsLost);
    }

    [Fact]
    public void Should_UseAverageTeamScore_When_TeamHasMultiplePlayers()
    {
        // Arrange
        var tournament = Any.Tournament();
        var highRated = Any.TournamentPlayer(tournament.Id, Guid.NewGuid(), p => p.Score = 1500);
        var lowRated = Any.TournamentPlayer(tournament.Id, Guid.NewGuid(), p => p.Score = 500);
        var opponent = Any.TournamentPlayer(tournament.Id, Guid.NewGuid(), p => p.Score = 1000);

        // Average team score = 1000 (same as opponent) → expected win = 0.5
        // Act: team wins, so delta should be positive ~K*0.5 = 16
        _calculator.UpdateScores([highRated, lowRated], [opponent], 5, 3, tournament);

        // Assert: both winners get positive delta
        Assert.True(highRated.ScoreDiff > 0);
        Assert.True(lowRated.ScoreDiff > 0);
    }
}
