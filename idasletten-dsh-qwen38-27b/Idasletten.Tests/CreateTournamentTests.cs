using Idasletten.Features.Common;
using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class CreateTournamentTests : IAsyncLifetime
{
    private TestDb _db = null!;

    public async Task InitializeAsync() => _db = await TestDb.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private IMediator Mediator => _db.Services.GetRequiredService<IMediator>();

    [Fact]
    public async Task Should_SetRoundNumberToTwo_When_ParentExists()
    {
        // Arrange
        var parent = Any.Tournament(parentTournamentId: null);
        await _db.AddTournamentAsync(parent);

        // Act
        var childId = await Mediator.Send(new CreateTournamentCommand(
            "Child Round", null, 1, 5, ScoreSystem.Elo, true, parent.Id, []));

        // Assert
        var child = await _db.Db.Tournaments.FirstAsync(t => t.Id == childId);
        Assert.Equal(2, child.RoundNumber);
        Assert.Equal(parent.Id, child.ParentTournamentId);
    }

    [Fact]
    public async Task Should_Throw_When_ParentHasItsOwnParent()
    {
        // Arrange — grandparent → parent → (new round not allowed)
        var grand = Any.Tournament();
        var parent = Any.Tournament(parentTournamentId: grand.Id);
        grand.RoundNumber = 1; parent.RoundNumber = 2;
        await _db.AddTournamentAsync(grand);
        await _db.AddTournamentAsync(parent);

        // Act / Assert
        var ex = await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new CreateTournamentCommand(
            "Bad Round", null, 1, 5, ScoreSystem.Elo, true, parent.Id, [])));
        Assert.Contains("round", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_AddCarriedOverPlayers_When_CreatedFromParent()
    {
        // Arrange
        var parent = Any.Tournament();
        var thomas = Any.User("THO");
        var lovi = Any.User("LOV");
        await _db.AddTournamentAsync(parent,
            (thomas, Any.Player(thomas, parent)), (lovi, Any.Player(lovi, parent)));

        // Act
        var childId = await Mediator.Send(new CreateTournamentCommand(
            "Round 2", null, 1, 5, ScoreSystem.Elo, true, parent.Id, [thomas.Id]));

        // Assert
        var carried = await _db.Db.TournamentPlayers
            .Where(p => p.TournamentId == childId).Select(p => p.UserId).ToListAsync();
        Assert.Equal(1, carried.Count);
        Assert.Contains(thomas.Id, carried);
        Assert.DoesNotContain(lovi.Id, carried);
    }

    [Fact]
    public async Task Should_Throw_When_CarriedOverPlayerNotInParent()
    {
        // Arrange
        var parent = Any.Tournament();
        var thu = Any.User("THO");
        await _db.AddTournamentAsync(parent, (thu, Any.Player(thu, parent)));
        var outsider = Any.User("ODF");

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new CreateTournamentCommand(
            "Round 2", null, 1, 5, ScoreSystem.Elo, true, parent.Id, [outsider.Id])));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public async Task Should_Throw_When_TeamSizeOutOfLimits(int teamSize)
    {
        // Arrange / Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new CreateTournamentCommand(
            "T", null, teamSize, 5, ScoreSystem.Elo, true, null, [])));
    }

    [Fact]
    public async Task Should_Throw_When_MaxPlayerCountBelowTwoTeams()
    {
        // Arrange / Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new CreateTournamentCommand(
            "T", 1, 2, 5, ScoreSystem.Elo, true, null, [])));
    }
}
