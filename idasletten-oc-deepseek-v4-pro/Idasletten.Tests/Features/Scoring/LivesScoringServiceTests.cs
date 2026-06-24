using Idasletten.Features.Scoring;
using Idasletten.Shared.Entities;

namespace Idasletten.Tests.Features.Scoring;

public class LivesScoringServiceTests
{
    [Fact]
    public void Should_LoseLife_When_LosingMatch()
    {
        var service = new LivesScoringService();
        var player = new TournamentPlayer { Id = Guid.NewGuid(), Score = 3, Lives = 3 };
        var opponent = new TournamentPlayer { Id = Guid.NewGuid(), Score = 3, Lives = 3 };

        var teams = new List<TournamentTeam>
        {
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = player.Id }] },
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = opponent.Id }] }
        };

        var results = new List<TournamentTeamMatchResult>
        {
            new() { TeamId = teams[0].Id, GoalsWon = 2, GoalsLost = 5 },
            new() { TeamId = teams[1].Id, GoalsWon = 5, GoalsLost = 2 }
        };

        service.Calculate(new TournamentMatch(), results, teams, [player, opponent]);

        Assert.Equal(2, player.Lives);
        Assert.Equal(2, player.Score);
        Assert.Equal(3, opponent.Lives);
    }

    [Fact]
    public void Should_NotGoBelowZero()
    {
        var service = new LivesScoringService();
        var player = new TournamentPlayer { Id = Guid.NewGuid(), Score = 0, Lives = 0 };

        var teams = new List<TournamentTeam>
        {
            new() { Id = Guid.NewGuid(), TeamPlayers = [new() { TournamentTeamId = Guid.NewGuid(), TournamentPlayerId = player.Id }] },
            new() { Id = Guid.NewGuid(), TeamPlayers = [] }
        };
        teams[1].TeamPlayers = [];

        var results = new List<TournamentTeamMatchResult>
        {
            new() { TeamId = teams[0].Id, GoalsWon = 0, GoalsLost = 5 },
            new() { TeamId = teams[1].Id, GoalsWon = 5, GoalsLost = 0 }
        };

        service.Calculate(new TournamentMatch(), results, teams, [player]);

        Assert.Equal(0, player.Lives);
        Assert.Equal(0, player.Score);
    }
}
