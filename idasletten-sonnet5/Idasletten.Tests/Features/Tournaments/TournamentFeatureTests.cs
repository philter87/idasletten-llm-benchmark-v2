using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Features.Tournaments.Queries.GetTournaments;
using Idasletten.Tests.TestData;
using MediatR;

namespace Idasletten.Tests.Features.Tournaments;

public class TournamentFeatureTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateTournamentWithDefaults_When_Created()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var name = Any.Word();

        // Act
        var tournamentId = await sender.Send(new CreateTournamentCommand(
            name, TeamSize: 2, PointsToWin: 5, ScoreSystem.WinCount, MaxPlayerCount: 10, IsPublic: true));

        // Assert
        var tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        Assert.NotNull(tournament);
        Assert.Equal(name, tournament!.Name);
        Assert.False(tournament.IsArchived);
        Assert.Equal(10, tournament.MaxPlayerCount);
    }

    [Fact]
    public async Task Should_ExcludeChildTournaments_When_IncludeChildrenIsFalse()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var parentId = await sender.Send(new CreateTournamentCommand(
            Any.Word(), 2, 5, ScoreSystem.Elo, null, IsPublic: true));
        var childId = await sender.Send(new CreateTournamentCommand(
            Any.Word(), 2, 5, ScoreSystem.Elo, null, IsPublic: true, ParentTournamentId: parentId));

        // Act
        var visible = await sender.Send(new GetTournamentsQuery(IncludeChildren: false));

        // Assert
        Assert.Contains(visible, t => t.Id == parentId);
        Assert.DoesNotContain(visible, t => t.Id == childId);
    }

    [Fact]
    public async Task Should_DropSeedTournament_When_ParentTournamentIsSet()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var seedId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var parentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));

        // Act: a tournament may be seeded only if it has no parent.
        var childId = await sender.Send(new CreateTournamentCommand(
            Any.Word(), 2, 5, ScoreSystem.Elo, null, true,
            SeedTournamentId: seedId, ParentTournamentId: parentId));

        // Assert
        var child = await sender.Send(new GetTournamentQuery(childId));
        Assert.Null(child!.SeedTournamentId);
    }
}
