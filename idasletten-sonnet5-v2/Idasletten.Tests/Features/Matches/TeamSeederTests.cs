using Idasletten.Features.Matches.Commands.PlanSeveralMatches;
using Idasletten.Shared.Entities;

namespace Idasletten.Tests.Features.Matches;

public class TeamSeederTests
{
    [Fact]
    public void Should_PairBestWithWorst_When_SeedingTypeIsEquality()
    {
        // Arrange: 10 ranked players, best (index 0) to worst (index 9).
        var players = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var teams = TeamSeeder.BuildTeams(players, SeedingType.Equality, teamSize: 2);

        // Assert: 1+10, 2+9, ...
        Assert.Equal(5, teams.Count);
        Assert.Equal([players[0], players[9]], teams[0]);
        Assert.Equal([players[1], players[8]], teams[1]);
    }

    [Fact]
    public void Should_PairTopHalfWithBottomHalf_When_SeedingTypeIsFair()
    {
        // Arrange: 10 ranked players, best (index 0) to worst (index 9).
        var players = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var teams = TeamSeeder.BuildTeams(players, SeedingType.Fair, teamSize: 2);

        // Assert: 1+6, 2+7, 3+8, 4+9, 5+10
        Assert.Equal(5, teams.Count);
        Assert.Equal([players[0], players[5]], teams[0]);
        Assert.Equal([players[1], players[6]], teams[1]);
        Assert.Equal([players[4], players[9]], teams[4]);
    }

    [Fact]
    public void Should_ProduceTeamsCoveringEveryPlayerExactlyOnce_When_SeedingTypeIsRandom()
    {
        // Arrange
        var players = Enumerable.Range(1, 8).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var teams = TeamSeeder.BuildTeams(players, SeedingType.Random, teamSize: 2);

        // Assert
        Assert.Equal(4, teams.Count);
        Assert.Equal(players.Count, teams.SelectMany(t => t).Distinct().Count());
    }
}
