using FluentAssertions;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Data;
using Idasletten.Shared.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Tests;

public class TournamentTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediator = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o =>
            o.UseInMemoryDatabase($"TournamentTests-{Guid.NewGuid()}"));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        services.AddScoped<DatabaseSeeder>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Seed baseline data
        var seeder = _serviceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task Should_CreateTournament_When_CommandIsValid()
    {
        // Arrange
        var command = new CreateTournamentCommand(
            "Test Tournament",
            TeamSize: 2,
            PointsToWin: 5,
            ScoreSystem: ScoreSystem.Elo,
            MaxPlayerCount: null,
            IsPublic: true,
            SeedTournamentId: null,
            ParentTournamentId: null);

        // Act
        var id = await _mediator.Send(command);

        // Assert
        id.Should().NotBe(Guid.Empty);

        var result = await _mediator.Send(new GetTournamentQuery(id));
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Tournament");
        result.TeamSize.Should().Be(2);
        result.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task Should_AddPlayerToTournament_When_PlayerDoesNotExist()
    {
        // Arrange
        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            "Player Test Tournament",
            2, 5, ScoreSystem.Elo, null, true, null, null));

        // Act
        var playerId = await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, "XYZ", "Test Player"));

        // Assert
        playerId.Should().NotBe(Guid.Empty);

        var tournament = await _mediator.Send(new GetTournamentQuery(tournamentId));
        tournament!.Players.Should().HaveCount(1);
        tournament.Players[0].Username.Should().Be("XYZ");
    }

    [Fact]
    public async Task Should_NotDuplicatePlayer_When_AddedTwice()
    {
        // Arrange
        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            "Dupe Test", 2, 5, ScoreSystem.Elo, null, true, null, null));
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, "DUP", null));

        // Act
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, "DUP", null));

        // Assert
        var tournament = await _mediator.Send(new GetTournamentQuery(tournamentId));
        tournament!.Players.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_RecordMatchResult_AndUpdateScores_When_TeamInputIsValid()
    {
        // Arrange — use TeamSize=1 so 1 player per team
        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            "Score Test", TeamSize: 1, PointsToWin: 5, ScoreSystem.Elo, null, true, null, null));
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, "AAA", "Player A"));
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, "BBB", "Player B"));

        var teams = new List<TeamInput>
        {
            new TeamInput(["AAA"], Goals: 5),
            new TeamInput(["BBB"], Goals: 2),
        };

        // Act
        var matchId = await _mediator.Send(new RecordMatchResultCommand(tournamentId, teams));

        // Assert
        matchId.Should().NotBe(Guid.Empty);
        var tournament = await _mediator.Send(new GetTournamentQuery(tournamentId));
        var playerA = tournament!.Players.Single(p => p.Username == "AAA");
        var playerB = tournament.Players.Single(p => p.Username == "BBB");

        playerA.WinCount.Should().Be(1);
        playerB.LoseCount.Should().Be(1);
        playerA.Score.Should().BeGreaterThan(1000); // Winner gains Elo
        playerB.Score.Should().BeLessThan(1000);    // Loser loses Elo
    }

    [Fact]
    public async Task Should_ListPublicTournaments_When_QueryIsPublicOnly()
    {
        // Arrange
        await _mediator.Send(new CreateTournamentCommand("Public T", 2, 5, ScoreSystem.Elo, null, true, null, null));
        await _mediator.Send(new CreateTournamentCommand("Private T", 2, 5, ScoreSystem.Elo, null, false, null, null));

        // Act
        var results = await _mediator.Send(new GetTournamentsQuery(false, false, false));

        // Assert
        results.Should().NotContain(t => t.Name == "Private T");
        results.Should().Contain(t => t.Name == "Public T");
    }
}
