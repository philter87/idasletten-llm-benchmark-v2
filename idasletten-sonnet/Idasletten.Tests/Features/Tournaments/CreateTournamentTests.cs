using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Tournaments;

public class CreateTournamentTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateTournament_When_ValidCommandIsGiven()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var id = await sender.Send(new CreateTournamentCommand(
            "Test Turnering",
            TeamSize: 2,
            PointsToWin: 5,
            ScoreSystem: ScoreSystem.Elo,
            MaxPlayerCount: null,
            IsPublic: true
        ));

        // Assert
        var tournament = await db.Tournaments.FindAsync(id);
        Assert.NotNull(tournament);
        Assert.Equal("Test Turnering", tournament.Name);
        Assert.Equal(2, tournament.TeamSize);
        Assert.Equal(ScoreSystem.Elo, tournament.ScoreSystem);
    }

    [Fact]
    public async Task Should_SetRoundNumber_When_CreatingChildTournament()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parentId = await sender.Send(new CreateTournamentCommand("Forældre", 2, 5, ScoreSystem.Elo, null, true));

        // Act
        var childId = await sender.Send(new CreateTournamentCommand("Barn", 2, 5, ScoreSystem.Elo, null, true, parentId));

        // Assert
        var child = await db.Tournaments.FindAsync(childId);
        Assert.NotNull(child);
        Assert.Equal(parentId, child.ParentTournamentId);
        Assert.Equal(2, child.RoundNumber);
    }
}
