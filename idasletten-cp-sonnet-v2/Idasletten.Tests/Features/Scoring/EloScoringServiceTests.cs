using Idasletten.Features.Scoring;
using Idasletten.Shared.Entities;

namespace Idasletten.Tests.Features.Scoring;

public class EloScoringServiceTests
{
    private readonly EloScoringService _service = new();

    [Fact]
    public void Should_IncreaseScore_When_PlayerWins()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var winner = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        var loser = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        winner.Score = 1000;
        winner.WinCount = 0;
        loser.Score = 1000;
        loser.LoseCount = 0;

        // Act
        _service.CalculateScores(
            [winner], [loser],
            5, 3,
            tournament);

        // Assert
        Assert.True(winner.Score > 1000);
        Assert.True(loser.Score < 1000);
        Assert.Equal(1, winner.WinCount);
        Assert.Equal(1, loser.LoseCount);
    }

    [Fact]
    public void Should_IncrementMatchCount_When_MatchIsPlayed()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var player1 = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        var player2 = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        player1.Score = 1200;
        player2.Score = 800;
        player1.MatchCount = 0;
        player2.MatchCount = 0;

        // Act
        _service.CalculateScores([player1], [player2], 5, 0, tournament);

        // Assert
        Assert.Equal(1, player1.MatchCount);
        Assert.Equal(1, player2.MatchCount);
    }

    [Fact]
    public void Should_GiveLargerGain_When_WinnerWasUnderdog()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var underdog = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        var favorite = Any.TournamentPlayer(Guid.NewGuid(), tournament.Id);
        underdog.Score = 800;
        favorite.Score = 1200;

        // Act
        _service.CalculateScores([underdog], [favorite], 5, 3, tournament);

        // Assert
        Assert.True(underdog.ScoreDiff > 16); // More than expected default K/2
    }
}
