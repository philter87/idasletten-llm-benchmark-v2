using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Idasletten.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Idasletten.Tests;

public class TournamentTests : IClassFixture<IdaslettenFactory>
{
    private readonly IdaslettenFactory _factory;
    public TournamentTests(IdaslettenFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_CreateTournament_When_CommandSent()
    {
        // Arrange
        var name = Any.String("ragnarok");

        // Act
        var id = await _factory.Send(new CreateTournamentCommand(
            name, TeamSize: 2, PointsToWin: 5, ScoreSystem.Elo, MaxPlayerCount: null, IsPublic: true));

        // Assert
        var detail = await _factory.Send(new GetTournamentDetailQuery(id));
        Assert.NotNull(detail);
        Assert.Equal(name, detail!.Name);
        Assert.Equal(ScoreSystem.Elo, detail.ScoreSystem);
    }

    [Fact]
    public async Task Should_NotIncludeChildRounds_When_ListingTournamentsByDefault()
    {
        // Arrange
        var parentId = await _factory.Send(new CreateTournamentCommand(
            Any.String("parent"), 2, 5, ScoreSystem.WinCount, null, true));
        var childId = await _factory.Send(new CreateTournamentCommand(
            Any.String("round2"), 2, 5, ScoreSystem.WinCount, null, true, ParentTournamentId: parentId));

        // Act
        var listed = await _factory.Send(new ListTournamentsQuery());

        // Assert
        Assert.Contains(listed, t => t.Id == parentId);
        Assert.DoesNotContain(listed, t => t.Id == childId);
    }

    [Fact]
    public async Task Should_SetRoundNumberTwo_When_CreatedFromParent()
    {
        // Arrange
        var parentId = await _factory.Send(new CreateTournamentCommand(
            Any.String("parent"), 2, 5, ScoreSystem.Elo, null, true));

        // Act
        var childId = await _factory.Send(new CreateTournamentCommand(
            Any.String("round2"), 2, 5, ScoreSystem.Elo, null, true, ParentTournamentId: parentId));

        // Assert
        await _factory.Query(async db =>
        {
            var child = await db.Tournaments.FirstAsync(t => t.Id == childId);
            Assert.Equal(2, child.RoundNumber);
            Assert.Null(child.SeedTournamentId); // a child may not be seeded
        });
    }
}
