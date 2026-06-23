using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Data;
using Idasletten.Tests.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features;

public class TournamentFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentFeatureTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_CreateTournament_When_CommandIsValid()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var tournamentId = await mediator.Send(new CreateTournamentCommand("Mjolner Cup", 2, 5, ScoreSystem.WinCount, null, true));
        var detail = await mediator.Send(new GetTournamentDetailQuery(tournamentId));

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("Mjolner Cup", detail.Name);
        Assert.Equal(ScoreSystem.WinCount, detail.ScoreSystem);
    }

    [Fact]
    public async Task Should_AutoCreateUser_When_PlayerInitialsAreNew()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tournamentId = await mediator.Send(new CreateTournamentCommand("New Player Cup", 2, 5, ScoreSystem.Elo, null, true));

        // Act
        await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, "abc", "Ada B C"));
        var detail = await mediator.Send(new GetTournamentDetailQuery(tournamentId));

        // Assert
        Assert.Contains(detail!.Players, player => player.Initials == "ABC" && player.Name == "Ada B C");
    }

    [Fact]
    public async Task Should_UpdateEloScore_When_MatchIsRecorded()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tournamentId = await mediator.Send(new CreateTournamentCommand("Elo Cup", 2, 5, ScoreSystem.Elo, null, true));

        // Act
        await mediator.Send(new RecordMatchCommand(tournamentId, null, ["AAA", "BBB"], ["CCC", "DDD"], 5, 2));
        var detail = await mediator.Send(new GetTournamentDetailQuery(tournamentId));

        // Assert
        Assert.Contains(detail!.Players, player => player.Initials == "AAA" && player.Wins == 1 && player.Score > 1000);
        Assert.Contains(detail.Players, player => player.Initials == "CCC" && player.Losses == 1 && player.Score < 1000);
    }
}
