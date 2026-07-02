using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class PlanningTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PlanningTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void Should_PairTopHalfWithBottomHalf_When_SeedingTypeIsFair()
    {
        // Arrange — 10 ranked players, best first.
        var players = Enumerable.Range(0, 10)
            .Select(i => new TournamentPlayer { Score = 1000 - i, User = Any.User() })
            .ToList();

        // Act
        var teams = PlanSeveralMatchesHandler.BuildTeams(players, 2, SeedingType.Fair, new Random(1));

        // Assert — example from the spec: 1+6, 2+7, 3+8, 4+9, 5+10.
        Assert.Equal(5, teams.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Same(players[i], teams[i][0]);
            Assert.Same(players[i + 5], teams[i][1]);
        }
    }

    [Fact]
    public void Should_PairBestWithWorst_When_SeedingTypeIsEquality()
    {
        // Arrange
        var players = Enumerable.Range(0, 10)
            .Select(i => new TournamentPlayer { Score = 1000 - i, User = Any.User() })
            .ToList();

        // Act
        var teams = PlanSeveralMatchesHandler.BuildTeams(players, 2, SeedingType.Equality, new Random(1));

        // Assert — 1+10, 2+9, 3+8, 4+7, 5+6.
        Assert.Equal(5, teams.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Same(players[i], teams[i][0]);
            Assert.Same(players[9 - i], teams[i][1]);
        }
    }

    [Fact]
    public void Should_UseAllPlayersExactlyOnce_When_SeedingTypeIsRandom()
    {
        // Arrange
        var players = Enumerable.Range(0, 8)
            .Select(_ => new TournamentPlayer { User = Any.User() })
            .ToList();

        // Act
        var teams = PlanSeveralMatchesHandler.BuildTeams(players, 2, SeedingType.Random, new Random(1));

        // Assert
        Assert.Equal(4, teams.Count);
        var used = teams.SelectMany(t => t).ToList();
        Assert.Equal(8, used.Distinct().Count());
    }

    [Fact]
    public async Task Should_CreateEnoughMatches_When_SeveralMatchesArePlanned()
    {
        // Arrange — 8 players, 2v2, 3 games each → 8*3 / 4 = 6 matches.
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tournament = await mediator.Send(new CreateTournamentCommand(Any.TournamentName(), TeamSize: 2));
        for (var i = 0; i < 8; i++)
            await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, Any.Initials()));

        // Act
        var matches = await mediator.Send(new PlanSeveralMatchesCommand(
            tournament.Id, GamesPerPlayer: 3, FixedTeams: false, SeedingType.Random));

        // Assert
        Assert.Equal(6, matches.Count);
        Assert.All(matches, m => Assert.Equal(MatchState.Planned, m.State));
        var planned = await db.TournamentMatches.CountAsync(
            m => m.TournamentId == tournament.Id && m.State == MatchState.Planned);
        Assert.Equal(6, planned);
    }

    [Fact]
    public async Task Should_SetSeedTournament_When_PlanningWithSeedForTheFirstTime()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await mediator.Send(new CreateTournamentCommand(Any.TournamentName()));
        var tournament = await mediator.Send(new CreateTournamentCommand(Any.TournamentName(), TeamSize: 1));
        for (var i = 0; i < 4; i++)
            await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, Any.Initials()));

        // Act
        await mediator.Send(new PlanSeveralMatchesCommand(
            tournament.Id, GamesPerPlayer: 1, FixedTeams: true, SeedingType.Fair, seed.Id));

        // Assert
        var reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(seed.Id, reloaded.SeedTournamentId);
    }
}
