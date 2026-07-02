using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class TournamentTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TournamentTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_CreateTournamentWithDefaults_When_OnlyNameIsGiven()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var name = Any.TournamentName();

        // Act
        var tournament = await mediator.Send(new CreateTournamentCommand(name));

        // Assert
        Assert.Equal(name, tournament.Name);
        Assert.Equal(2, tournament.TeamSize);
        Assert.Equal(5, tournament.PointsToWin);
        Assert.Equal(ScoreSystem.Elo, tournament.ScoreSystem);
        Assert.Null(tournament.MaxPlayerCount);
    }

    [Fact]
    public async Task Should_CreateUserAutomatically_When_PlayerWithUnknownInitialsIsAdded()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tournament = await mediator.Send(new CreateTournamentCommand(Any.TournamentName()));
        var initials = Any.Initials();

        // Act
        var player = await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, initials));

        // Assert
        var user = await db.Users.SingleAsync(u => u.Id == player.UserId);
        Assert.Equal(initials, user.UserName);
    }

    [Fact]
    public async Task Should_ReturnSamePlayer_When_PlayerIsAddedTwice()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tournament = await mediator.Send(new CreateTournamentCommand(Any.TournamentName()));
        var initials = Any.Initials();

        // Act
        var first = await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, initials));
        var second = await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, initials));

        // Assert
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Should_RejectPlayer_When_MaxPlayerCountIsReached()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tournament = await mediator.Send(new CreateTournamentCommand(
            Any.TournamentName(), MaxPlayerCount: 1));
        await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, Any.Initials()));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, Any.Initials())));
    }

    [Fact]
    public async Task Should_CarryPlayersWithResetScores_When_NextRoundIsCreated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parent = await mediator.Send(new CreateTournamentCommand(Any.TournamentName()));
        for (var i = 0; i < 4; i++)
            await mediator.Send(new AddPlayerToTournamentCommand(parent.Id, Any.Initials()));

        // Act
        var child = await mediator.Send(new CreateNextRoundCommand(parent.Id, TopPlayerCount: 2));

        // Assert
        Assert.Equal(parent.Id, child.ParentTournamentId);
        Assert.Equal(2, child.RoundNumber);
        var childPlayers = await db.TournamentPlayers.Where(p => p.TournamentId == child.Id).ToListAsync();
        Assert.Equal(2, childPlayers.Count);
        Assert.All(childPlayers, p => Assert.Equal(0, p.MatchCount));
    }

    [Fact]
    public async Task Should_RejectSeedTournament_When_TournamentHasParent()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var parent = await mediator.Send(new CreateTournamentCommand(Any.TournamentName()));
        await mediator.Send(new AddPlayerToTournamentCommand(parent.Id, Any.Initials()));
        var child = await mediator.Send(new CreateNextRoundCommand(parent.Id));
        var other = await mediator.Send(new CreateTournamentCommand(Any.TournamentName()));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Send(new SetSeedTournamentCommand(child.Id, other.Id)));
    }
}
