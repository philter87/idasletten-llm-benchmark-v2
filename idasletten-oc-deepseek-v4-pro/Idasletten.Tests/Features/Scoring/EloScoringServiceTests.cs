using Idasletten.Features.Scoring;
using Idasletten.Shared.Entities;

namespace Idasletten.Tests.Features.Scoring;

public class EloScoringServiceTests
{
    [Fact]
    public void Should_IncreaseScore_When_PlayerWins()
    {
        var service = new EloScoringService();
        var player = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1000 };
        var opponent = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1000 };

        var teams = new List<TournamentTeam>
        {
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = player.Id }] },
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = opponent.Id }] }
        };

        var results = new List<TournamentTeamMatchResult>
        {
            new() { TeamId = teams[0].Id, GoalsWon = 5, GoalsLost = 3 },
            new() { TeamId = teams[1].Id, GoalsWon = 3, GoalsLost = 5 }
        };

        service.Calculate(new TournamentMatch(), results, teams, [player, opponent]);

        Assert.True(player.Score > 1000);
        Assert.True(opponent.Score < 1000);
    }

    [Fact]
    public void Should_IncrementMatchCount_OnCalculate()
    {
        var service = new EloScoringService();
        var player = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1000 };
        var opponent = new TournamentPlayer { Id = Guid.NewGuid(), Score = 1000 };

        var teams = new List<TournamentTeam>
        {
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = player.Id }] },
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = opponent.Id }] }
        };

        var results = new List<TournamentTeamMatchResult>
        {
            new() { TeamId = teams[0].Id, GoalsWon = 5, GoalsLost = 0 },
            new() { TeamId = teams[1].Id, GoalsWon = 0, GoalsLost = 5 }
        };

        service.Calculate(new TournamentMatch(), results, teams, [player, opponent]);

        Assert.Equal(1000 + 16, player.Score, tolerance: 1);
        Assert.Equal(1000 - 16, opponent.Score, tolerance: 1);
    }
}
