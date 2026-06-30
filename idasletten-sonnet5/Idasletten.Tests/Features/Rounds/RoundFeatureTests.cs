using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands.CreatePlannedMatch;
using Idasletten.Features.Matches.Commands.SaveMatch;
using Idasletten.Features.Rounds.Commands.CreateNextRound;
using Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Tests.TestData;
using MediatR;

namespace Idasletten.Tests.Features.Rounds;

public class RoundFeatureTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Should_CarryOverPlayersWithResetScores_When_NextRoundIsCreated()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var parentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var matchId = await sender.Send(new CreatePlannedMatchCommand(parentId));
        var winner = Any.Username();
        var loser = Any.Username();
        await sender.Send(new SaveMatchCommand(
            matchId, parentId, [new TeamInput([winner], 5), new TeamInput([loser], 1)], RecordResult: true));

        // Act
        var roundTwoId = await sender.Send(new CreateNextRoundCommand(parentId, Any.Word()));

        // Assert
        var roundTwo = await sender.Send(new GetTournamentQuery(roundTwoId));
        Assert.Equal(1, roundTwo!.RoundNumber);
        Assert.Equal(parentId, roundTwo.ParentTournamentId);

        var roundTwoPlayers = await sender.Send(new GetTournamentPlayersQuery(roundTwoId));
        Assert.Equal(2, roundTwoPlayers.Count);
        Assert.All(roundTwoPlayers, p => Assert.Equal(1200, p.Score));
    }

    [Fact]
    public async Task Should_OnlyCarryOverTopN_When_TopNIsSpecified()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var parentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.WinCount, null, true));
        var matchId = await sender.Send(new CreatePlannedMatchCommand(parentId));
        await sender.Send(new SaveMatchCommand(
            matchId, parentId, [new TeamInput([Any.Username()], 5), new TeamInput([Any.Username()], 1)], RecordResult: true));

        // Act
        var roundTwoId = await sender.Send(new CreateNextRoundCommand(parentId, Any.Word(), TopN: 1));

        // Assert
        var roundTwoPlayers = await sender.Send(new GetTournamentPlayersQuery(roundTwoId));
        Assert.Single(roundTwoPlayers);
    }
}
