using Idasletten.Data;
using Idasletten.Features.Players.Commands.AddPlayerToTournament;
using Idasletten.Tests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Players;

public class AddPlayerToTournamentTests(IdaslettenWebApplicationFactory factory) : IClassFixture<IdaslettenWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateNewUser_When_InitialsHaveNotBeenUsedBefore()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var tournament = Any.Tournament();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var initials = Any.Initials();

        // Act
        var playerId = await sender.Send(new AddPlayerToTournamentCommand(tournament.Id, initials));

        // Assert
        var player = await db.TournamentPlayers.Include(p => p.User).FirstAsync(p => p.Id == playerId);
        Assert.Equal(initials, player.User.UserName);
        Assert.Equal(1000, player.Score);
    }

    [Fact]
    public async Task Should_ReturnExistingPlayer_When_PlayerIsAlreadyInTournament()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var tournament = Any.Tournament();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var initials = Any.Initials();
        var firstId = await sender.Send(new AddPlayerToTournamentCommand(tournament.Id, initials));

        // Act
        var secondId = await sender.Send(new AddPlayerToTournamentCommand(tournament.Id, initials));

        // Assert
        Assert.Equal(firstId, secondId);
        Assert.Equal(1, await db.TournamentPlayers.CountAsync(p => p.TournamentId == tournament.Id));
    }

    [Fact]
    public async Task Should_Throw_When_TournamentAlreadyHasMaxPlayers()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var tournament = Any.Tournament(maxPlayerCount: 1);
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        await sender.Send(new AddPlayerToTournamentCommand(tournament.Id, Any.Initials()));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new AddPlayerToTournamentCommand(tournament.Id, Any.Initials())));
    }
}
