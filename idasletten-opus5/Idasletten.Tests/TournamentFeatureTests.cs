using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Features.Users.Commands;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class TournamentFeatureTests(IdaslettenFactory factory) : IClassFixture<IdaslettenFactory>, IAsyncLifetime
{
    /// <summary>Makes sure the database is migrated and seeded before the first test runs.</summary>
    public Task InitializeAsync() => factory.InitialiseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_CreateUser_When_PlayerIsAddedWithUnknownInitials()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(new CreateTournament(Any.Tournament().Name));
        var initials = Any.Initials();

        // Act
        await factory.SendAsync(new AddPlayerToTournament(tournamentId, initials, "Ny Viking"));

        // Assert
        var user = await factory.QueryAsync(db =>
            db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == initials.ToUpperInvariant()));

        Assert.NotNull(user);
        Assert.Equal("Ny Viking", user!.Name);
    }

    [Fact]
    public async Task Should_ReuseTheSameUser_When_TheInitialsAreAlreadyKnown()
    {
        // Arrange
        var initials = Any.Initials();
        var first = await factory.SendAsync(new GetOrCreateUser(initials));

        // Act - same initials written in lower case.
        var second = await factory.SendAsync(new GetOrCreateUser(initials.ToLowerInvariant()));

        // Assert
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Should_AddThePlayerOnlyOnce_When_TheSameInitialsAreAddedTwice()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(new CreateTournament(Any.Tournament().Name));
        var initials = Any.Initials();

        // Act
        var first = await factory.SendAsync(new AddPlayerToTournament(tournamentId, initials));
        var second = await factory.SendAsync(new AddPlayerToTournament(tournamentId, initials));

        // Assert
        Assert.Equal(first, second);
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));
        Assert.Single(scoreboard);
    }

    [Fact]
    public async Task Should_RefusePlayer_When_TheTournamentIsFull()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(
            new CreateTournament(Any.Tournament().Name, MaxPlayerCount: 2));
        await factory.SendAsync(new AddPlayerToTournament(tournamentId, Any.Initials()));
        await factory.SendAsync(new AddPlayerToTournament(tournamentId, Any.Initials()));

        // Act
        var full = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.SendAsync(new AddPlayerToTournament(tournamentId, Any.Initials())));

        // Assert
        Assert.Contains("full", full.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_StartEveryPlayerWithThreeLives_When_TheTournamentUsesLives()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(
            new CreateTournament(Any.Tournament().Name, ScoreSystem: ScoreSystem.Lives));

        // Act
        await factory.SendAsync(new AddPlayerToTournament(tournamentId, Any.Initials()));

        // Assert
        var player = (await factory.SendAsync(new GetScoreboard(tournamentId))).Single();
        Assert.Equal(3, player.Lives);
        Assert.Equal(3, player.Score);
    }

    [Fact]
    public async Task Should_NotSetLives_When_TheTournamentDoesNotUseLives()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(
            new CreateTournament(Any.Tournament().Name, ScoreSystem: ScoreSystem.Elo));

        // Act
        await factory.SendAsync(new AddPlayerToTournament(tournamentId, Any.Initials()));

        // Assert
        var player = (await factory.SendAsync(new GetScoreboard(tournamentId))).Single();
        Assert.Equal(0, player.Lives);
    }

    [Fact]
    public async Task Should_CarryOverTheBestPlayers_When_ANewRoundIsCreated()
    {
        // Arrange - a tournament where one team is clearly better.
        var parentId = await factory.SendAsync(new CreateTournament("Runde 1"));
        var initials = Enumerable.Range(0, 4).Select(_ => Any.Initials()).Distinct().ToList();
        foreach (var player in initials)
        {
            await factory.SendAsync(new AddPlayerToTournament(parentId, player));
        }

        await factory.SendAsync(new SaveMatch(parentId, Guid.NewGuid(),
        [
            new MatchTeamInput([initials[0], initials[1]], 10),
            new MatchTeamInput([initials[2], initials[3]], 3),
        ]));

        // Act
        var roundTwoId = await factory.SendAsync(new CreateTournament(
            "Runde 2", ParentTournamentId: parentId, AdvancingPlayerCount: 2));

        // Assert
        var roundTwo = await factory.SendAsync(new GetTournament(roundTwoId));
        var players = await factory.SendAsync(new GetScoreboard(roundTwoId));

        Assert.Equal(2, roundTwo!.RoundNumber);
        Assert.Equal(2, players.Count);
        Assert.All(players, player => Assert.Equal(0, player.MatchCount));
        Assert.All(players, player => Assert.Contains(player.Initials, initials.Take(2)));
    }

    [Fact]
    public async Task Should_ResetTheScore_When_PlayersAdvanceToTheNextRound()
    {
        // Arrange
        var parentId = await factory.SendAsync(new CreateTournament("Elo runde 1"));
        var initials = Enumerable.Range(0, 4).Select(_ => Any.Initials()).Distinct().ToList();
        foreach (var player in initials)
        {
            await factory.SendAsync(new AddPlayerToTournament(parentId, player));
        }

        await factory.SendAsync(new SaveMatch(parentId, Guid.NewGuid(),
        [
            new MatchTeamInput([initials[0], initials[1]], 10),
            new MatchTeamInput([initials[2], initials[3]], 3),
        ]));

        // Act
        var roundTwoId = await factory.SendAsync(
            new CreateTournament("Elo runde 2", ParentTournamentId: parentId));

        // Assert
        var players = await factory.SendAsync(new GetScoreboard(roundTwoId));
        Assert.All(players, player => Assert.Equal(1200, player.Score));
    }

    [Fact]
    public async Task Should_IgnoreTheSeedTournament_When_TheTournamentHasAParent()
    {
        // Arrange
        var seedId = await factory.SendAsync(new CreateTournament("Seed"));
        var parentId = await factory.SendAsync(new CreateTournament("Forælder"));

        // Act
        var roundId = await factory.SendAsync(new CreateTournament(
            "Runde 2", SeedTournamentId: seedId, ParentTournamentId: parentId));

        // Assert - a tournament may only be seeded when it has no parent.
        var round = await factory.SendAsync(new GetTournament(roundId));
        Assert.Null(round!.SeedTournamentId);
        Assert.False(round.CanBeSeeded);
    }

    [Fact]
    public async Task Should_RefuseSeeding_When_TheTournamentIsARound()
    {
        // Arrange
        var parentId = await factory.SendAsync(new CreateTournament("Forælder"));
        var seedId = await factory.SendAsync(new CreateTournament("Seed"));
        var roundId = await factory.SendAsync(
            new CreateTournament("Runde 2", ParentTournamentId: parentId));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.SendAsync(new SetSeedTournament(roundId, seedId)));
    }

    [Fact]
    public async Task Should_HideRounds_When_TournamentsAreListed()
    {
        // Arrange
        var parentId = await factory.SendAsync(new CreateTournament(Any.Tournament().Name));
        var roundId = await factory.SendAsync(new CreateTournament(
            "Skjult runde", ParentTournamentId: parentId));

        // Act
        var withoutRounds = await factory.SendAsync(new GetTournaments());
        var withRounds = await factory.SendAsync(new GetTournaments(IncludeRounds: true));

        // Assert
        Assert.DoesNotContain(withoutRounds, summary => summary.Id == roundId);
        Assert.Contains(withRounds, summary => summary.Id == roundId);
    }

    [Fact]
    public async Task Should_OnlyReturnPublicAndOpenTournaments_When_TheFrontPageAsks()
    {
        // Arrange
        var publicId = await factory.SendAsync(new CreateTournament("Offentlig", IsPublic: true));
        var privateId = await factory.SendAsync(new CreateTournament("Privat", IsPublic: false));
        var archivedId = await factory.SendAsync(new CreateTournament("Arkiveret", IsPublic: true));
        await factory.SendAsync(new SetTournamentArchived(archivedId, true));

        // Act
        var summaries = await factory.SendAsync(
            new GetTournaments(OnlyPublic: true, IncludeArchived: false));

        // Assert
        Assert.Contains(summaries, summary => summary.Id == publicId);
        Assert.DoesNotContain(summaries, summary => summary.Id == privateId);
        Assert.DoesNotContain(summaries, summary => summary.Id == archivedId);
    }

    [Fact]
    public async Task Should_ListPlayersOfAPreviousTournament_When_AddingPlayersFromIt()
    {
        // Arrange
        var sourceId = await factory.SendAsync(new CreateTournament("Kilde"));
        var initials = Enumerable.Range(0, 3).Select(_ => Any.Initials()).Distinct().ToList();
        foreach (var player in initials)
        {
            await factory.SendAsync(new AddPlayerToTournament(sourceId, player));
        }

        var targetId = await factory.SendAsync(new CreateTournament("Ny"));
        var firstUserId = (await factory.SendAsync(new GetScoreboard(sourceId))).First().UserId;

        // Act
        await factory.SendAsync(new AddPlayersFromTournament(targetId, sourceId, [firstUserId]));
        var candidates = await factory.SendAsync(new GetPlayersFromTournament(sourceId, targetId));

        // Assert
        Assert.Equal(3, candidates.Count);
        Assert.True(candidates.Single(candidate => candidate.UserId == firstUserId).IsAlreadyAdded);
        Assert.Equal(1, (await factory.SendAsync(new GetScoreboard(targetId))).Count);
    }

    [Fact]
    public async Task Should_SeedTheDatabase_When_TheApplicationStarts()
    {
        // Arrange + Act
        var tournaments = await factory.SendAsync(new GetTournaments());

        // Assert - the seeder runs both locally and in the tests.
        Assert.Contains(tournaments, summary => summary.Name == "Ragnarok Cup 2026");
        Assert.Contains(tournaments, summary => summary is { Name: "Midgard Mesterskab 2025", IsArchived: true });
    }
}
