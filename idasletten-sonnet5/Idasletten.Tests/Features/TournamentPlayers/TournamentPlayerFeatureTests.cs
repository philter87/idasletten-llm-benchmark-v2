using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;
using Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;
using Idasletten.Tests.TestData;
using MediatR;

namespace Idasletten.Tests.Features.TournamentPlayers;

public class TournamentPlayerFeatureTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateUserAndPlayer_When_UsernameHasNotBeenSeenBefore()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var username = Any.Username();

        // Act
        await sender.Send(new AddPlayerToTournamentCommand(tournamentId, username));

        // Assert
        var players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        Assert.Contains(players, p => p.Username == username);
    }

    [Fact]
    public async Task Should_ReturnSameTournamentPlayer_When_SameUsernameAddedTwice()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var username = Any.Username();

        // Act
        var firstPlayerId = await sender.Send(new AddPlayerToTournamentCommand(tournamentId, username));
        var secondPlayerId = await sender.Send(new AddPlayerToTournamentCommand(tournamentId, username));

        // Assert
        Assert.Equal(firstPlayerId, secondPlayerId);
        var players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        Assert.Single(players.Where(p => p.Username == username));
    }

    [Fact]
    public async Task Should_ThrowInvalidOperation_When_MaxPlayerCountIsReached()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, MaxPlayerCount: 1, true));
        await sender.Send(new AddPlayerToTournamentCommand(tournamentId, Any.Username()));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new AddPlayerToTournamentCommand(tournamentId, Any.Username())));
    }

    [Fact]
    public async Task Should_InitializeEloScoreToStartingRating_When_PlayerJoinsEloTournament()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var username = Any.Username();

        // Act
        await sender.Send(new AddPlayerToTournamentCommand(tournamentId, username));

        // Assert
        var players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        Assert.Equal(1200, players.Single(p => p.Username == username).Score);
    }
}
