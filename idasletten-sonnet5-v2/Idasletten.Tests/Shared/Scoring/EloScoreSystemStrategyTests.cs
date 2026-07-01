using Idasletten.Shared.Scoring;
using Idasletten.Tests.TestSupport;

namespace Idasletten.Tests.Shared.Scoring;

public class EloScoreSystemStrategyTests
{
    [Fact]
    public void Should_IncreaseWinnerAndDecreaseLoser_When_RatingsAreEqual()
    {
        // Arrange
        var strategy = new EloScoreSystemStrategy();
        var tournamentId = Guid.NewGuid();
        var winner = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), score: 1000);
        var loser = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), score: 1000);
        var teams = new List<TeamMatchInfo>
        {
            new() { TeamId = Guid.NewGuid(), Players = [winner], GoalsWon = 5, GoalsLost = 2 },
            new() { TeamId = Guid.NewGuid(), Players = [loser], GoalsWon = 2, GoalsLost = 5 },
        };

        // Act
        strategy.ApplyMatch(teams);

        // Assert
        Assert.Equal(1016, winner.Score);
        Assert.Equal(984, loser.Score);
    }

    [Fact]
    public void Should_AverageTeamRating_When_TeamHasMultiplePlayers()
    {
        // Arrange
        var strategy = new EloScoreSystemStrategy();
        var tournamentId = Guid.NewGuid();
        var strongPlayer = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), score: 1200);
        var weakPlayer = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), score: 800);
        var opponent1 = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), score: 1000);
        var opponent2 = Any.TournamentPlayer(tournamentId, Guid.NewGuid(), score: 1000);
        var teams = new List<TeamMatchInfo>
        {
            new() { TeamId = Guid.NewGuid(), Players = [strongPlayer, weakPlayer], GoalsWon = 5, GoalsLost = 3 },
            new() { TeamId = Guid.NewGuid(), Players = [opponent1, opponent2], GoalsWon = 3, GoalsLost = 5 },
        };

        // Act
        strategy.ApplyMatch(teams);

        // Assert: team average (1000) vs 1000 means expected 0.5, so both teammates get the same delta.
        Assert.Equal(strongPlayer.Score - 1200, weakPlayer.Score - 800);
    }
}
