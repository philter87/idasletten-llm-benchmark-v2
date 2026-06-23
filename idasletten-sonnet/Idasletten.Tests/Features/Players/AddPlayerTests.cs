using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Players;

public class AddPlayerTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateUserAndPlayer_When_InitialsAreNew()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tournamentId = await sender.Send(new CreateTournamentCommand("T1", 2, 5, ScoreSystem.Elo, null, true));

        // Act
        var playerId = await sender.Send(new AddPlayerCommand(tournamentId, "TST", "Test Spiller"));

        // Assert
        var player = await db.TournamentPlayers.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == playerId);
        Assert.NotNull(player);
        Assert.Equal("TST", player.User.Username);
        Assert.Equal("Test Spiller", player.User.Name);
    }

    [Fact]
    public async Task Should_NotDuplicatePlayer_When_SameInitialsAddedTwice()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tournamentId = await sender.Send(new CreateTournamentCommand("T2", 2, 5, ScoreSystem.Elo, null, true));
        await sender.Send(new AddPlayerCommand(tournamentId, "DUP", "Dup Spiller"));

        // Act
        await sender.Send(new AddPlayerCommand(tournamentId, "DUP", "Dup Spiller"));

        // Assert
        var count = await db.TournamentPlayers.CountAsync(p => p.TournamentId == tournamentId);
        Assert.Equal(1, count);
    }
}
