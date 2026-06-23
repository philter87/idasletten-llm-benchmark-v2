using Idasletten.Data;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Idasletten.Tests;

public class PlanningTests : IClassFixture<IdaslettenFactory>
{
    private readonly IdaslettenFactory _factory;
    public PlanningTests(IdaslettenFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_CreatePlannedMatches_When_PlanSeveralRequested()
    {
        // Arrange — a tournament with eight players.
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Elo, null, true));
        for (int i = 0; i < 8; i++)
            await _factory.Send(new AddPlayerCommand(id, $"P{i:D2}"));

        // Act — three games per player, random seeding.
        var count = await _factory.Send(new PlanMatchesCommand(id, GamesPerPlayer: 3, FixedTeam: false, SeedingType.Random));

        // Assert — matches were created and are all Planned.
        Assert.True(count > 0);
        await _factory.Query(async db =>
        {
            var planned = await db.TournamentMatches
                .CountAsync(m => m.TournamentId == id && m.State == MatchState.Planned);
            Assert.Equal(count, planned);
        });
    }

    [Fact]
    public async Task Should_RecordSeedTournament_When_PlanningWithSeed()
    {
        // Arrange — a seed tournament and a fresh one with players.
        var seedId = await _factory.Send(new CreateTournamentCommand(
            Any.String("seed"), 2, 5, ScoreSystem.Elo, null, true));
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Elo, null, true));
        for (int i = 0; i < 4; i++)
            await _factory.Send(new AddPlayerCommand(id, $"S{i:D2}"));

        // Act
        await _factory.Send(new PlanMatchesCommand(id, 1, false, SeedingType.Equality, seedId));

        // Assert
        await _factory.Query(async db =>
        {
            var t = await db.Tournaments.FirstAsync(x => x.Id == id);
            Assert.Equal(seedId, t.SeedTournamentId);
        });
    }
}
