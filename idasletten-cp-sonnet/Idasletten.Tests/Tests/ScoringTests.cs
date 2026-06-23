using FluentAssertions;
using Idasletten.Features.Scoring;

namespace Idasletten.Tests.Tests;

public class ScoringTests
{
    [Fact]
    public void Should_IncreaseWinnerScore_When_EloMatchIsRecorded()
    {
        // Arrange
        var calculator = new EloScoreCalculator();
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var currentScores = new Dictionary<Guid, double>
        {
            [winnerId] = 1000.0,
            [loserId] = 1000.0,
        };
        var results = new List<PlayerMatchResult>
        {
            new PlayerMatchResult(winnerId, GoalsWon: 5, GoalsLost: 2, Won: true),
            new PlayerMatchResult(loserId, GoalsWon: 2, GoalsLost: 5, Won: false),
        };

        // Act
        var updates = calculator.CalculateScores(results, currentScores);

        // Assert
        var winnerUpdate = updates.Single(u => u.PlayerId == winnerId);
        var loserUpdate = updates.Single(u => u.PlayerId == loserId);
        winnerUpdate.NewScore.Should().BeGreaterThan(1000.0);
        loserUpdate.NewScore.Should().BeLessThan(1000.0);
        winnerUpdate.ScoreDiff.Should().BeGreaterThan(0);
        loserUpdate.ScoreDiff.Should().BeLessThan(0);
    }

    [Fact]
    public void Should_DecrementLoserLives_When_LivesMatchIsRecorded()
    {
        // Arrange
        var calculator = new LivesScoreCalculator();
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var currentScores = new Dictionary<Guid, double>
        {
            [winnerId] = 3.0,
            [loserId] = 3.0,
        };
        var results = new List<PlayerMatchResult>
        {
            new PlayerMatchResult(winnerId, GoalsWon: 5, GoalsLost: 0, Won: true),
            new PlayerMatchResult(loserId, GoalsWon: 0, GoalsLost: 5, Won: false),
        };

        // Act
        var updates = calculator.CalculateScores(results, currentScores);

        // Assert
        updates.Single(u => u.PlayerId == winnerId).NewScore.Should().Be(3.0);
        updates.Single(u => u.PlayerId == loserId).NewScore.Should().Be(2.0);
    }

    [Fact]
    public void Should_IncrementWinnerScore_When_WinCountMatchIsRecorded()
    {
        // Arrange
        var calculator = new WinCountScoreCalculator();
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var currentScores = new Dictionary<Guid, double>
        {
            [winnerId] = 5.0,
            [loserId] = 3.0,
        };
        var results = new List<PlayerMatchResult>
        {
            new PlayerMatchResult(winnerId, GoalsWon: 5, GoalsLost: 1, Won: true),
            new PlayerMatchResult(loserId, GoalsWon: 1, GoalsLost: 5, Won: false),
        };

        // Act
        var updates = calculator.CalculateScores(results, currentScores);

        // Assert
        updates.Single(u => u.PlayerId == winnerId).NewScore.Should().Be(6.0);
        updates.Single(u => u.PlayerId == loserId).NewScore.Should().Be(3.0);
    }

    [Fact]
    public void Should_CalculateTrueSkillScores_When_MatchIsRecorded()
    {
        // Arrange
        var calculator = new TrueSkillScoreCalculator();
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var defaultScore = (25.0 - 3.0 * 8.333) * 100.0;
        var currentScores = new Dictionary<Guid, double>
        {
            [winnerId] = defaultScore,
            [loserId] = defaultScore,
        };
        var results = new List<PlayerMatchResult>
        {
            new PlayerMatchResult(winnerId, GoalsWon: 5, GoalsLost: 0, Won: true),
            new PlayerMatchResult(loserId, GoalsWon: 0, GoalsLost: 5, Won: false),
        };

        // Act
        var updates = calculator.CalculateScores(results, currentScores);

        // Assert
        updates.Should().HaveCount(2);
        updates.Single(u => u.PlayerId == winnerId).ScoreDiff.Should().BeGreaterThan(0);
        updates.Single(u => u.PlayerId == loserId).ScoreDiff.Should().BeLessThan(0);
    }
}
