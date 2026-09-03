using Idasletten.Features.Common;
using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Players.Commands.RemovePlayer;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class PlayerCommandTests : IAsyncLifetime
{
    private TestDb _db = null!;

    public async Task InitializeAsync() => _db = await TestDb.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private IMediator Mediator => _db.Services.GetRequiredService<IMediator>();

    [Fact]
    public async Task Should_CreateUserAndPlayer_When_InitialsAreNew()
    {
        // Arrange
        var t = Any.Tournament();
        await _db.AddTournamentAsync(t);

        // Act
        var added = await Mediator.Send(new AddPlayerCommand(t.Id, "tho", "Thor Odinson"));

        // Assert — normalized to uppercase
        Assert.Equal("THO", added.Initials);
        var user = await _db.Db.Users.SingleOrDefaultAsync(u => u.Username == "THO");
        Assert.NotNull(user);
        Assert.Equal("Thor Odinson", user.Name);
        Assert.Equal(1, await _db.Db.TournamentPlayers.CountAsync(p => p.TournamentId == t.Id));
    }

    [Fact]
    public async Task Should_Throw_When_PlayerAlreadyInTournament()
    {
        // Arrange
        var t = Any.Tournament();
        await _db.AddTournamentAsync(t);
        await Mediator.Send(new AddPlayerCommand(t.Id, "THO"));

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new AddPlayerCommand(t.Id, "THO")));
    }

    [Fact]
    public async Task Should_Throw_When_MaxPlayerCountReached()
    {
        // Arrange
        var t = Any.Tournament(maxPlayerCount: 2);
        await _db.AddTournamentAsync(t);
        await Mediator.Send(new AddPlayerCommand(t.Id, "THO"));
        await Mediator.Send(new AddPlayerCommand(t.Id, "LOV"));

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new AddPlayerCommand(t.Id, "ODF")));
    }

    [Fact]
    public async Task Should_RejectInitials_When_ShorterThanTwoCharacters()
    {
        // Arrange
        var t = Any.Tournament();
        await _db.AddTournamentAsync(t);

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new AddPlayerCommand(t.Id, "T")));
    }

    [Fact]
    public async Task Should_Throw_When_RemovingPlayerWithPlayedMatches()
    {
        // Arrange
        var t = Any.Tournament();
        var thu = Any.User("THO");
        var player = Any.Player(thu, t);
        player.MatchCount = 2;
        await _db.AddTournamentAsync(t, (thu, player));

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(
            new RemovePlayerCommand(t.Id, player.Id)));
    }

    [Fact]
    public async Task Should_RemovePlayer_When_NoMatchesPlayed()
    {
        // Arrange
        var t = Any.Tournament();
        var thu = Any.User("THO");
        var player = Any.Player(thu, t);
        await _db.AddTournamentAsync(t, (thu, player));

        // Act
        await Mediator.Send(new RemovePlayerCommand(t.Id, player.Id));

        // Assert
        Assert.Equal(0, await _db.Db.TournamentPlayers.CountAsync(p => p.TournamentId == t.Id));
    }

    [Fact]
    public async Task Should_Throw_When_TournamentIsArchived()
    {
        // Arrange
        var t = Any.Tournament(isArchived: true);
        await _db.AddTournamentAsync(t);

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new AddPlayerCommand(t.Id, "THO")));
    }
}
