using Idasletten.Features.Scoring;
using Idasletten.Shared.Entities;

namespace Idasletten.Tests.Features.Scoring;

public class WinCountScoringServiceTests
{
    [Fact]
    public void Should_IncrementWinCount_When_Winning()
    {
        var service = new WinCountScoringService();
        var player = new TournamentPlayer { Id = Guid.NewGuid(), WinCount = 0, Score = 0 };
        var opponent = new TournamentPlayer { Id = Guid.NewGuid(), WinCount = 0, Score = 0 };

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

        Assert.Equal(1, player.WinCount);
        Assert.Equal(1, player.Score);
        Assert.Equal(0, opponent.WinCount);
    }
}
