using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class MatchFeatureTests(IdaslettenFactory factory) : IClassFixture<IdaslettenFactory>, IAsyncLifetime
{
    /// <summary>Makes sure the database is migrated and seeded before the first test runs.</summary>
    public Task InitializeAsync() => factory.InitialiseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_UpdateTheScoreboard_When_AResultIsSaved()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);

        // Act
        await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([players[0], players[1]], 10),
            new MatchTeamInput([players[2], players[3]], 6),
        ]));

        // Assert
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));
        var winner = scoreboard.Single(row => row.Initials == players[0]);
        var loser = scoreboard.Single(row => row.Initials == players[2]);

        Assert.Equal(1216, winner.Score, 3);
        Assert.Equal(1184, loser.Score, 3);
        Assert.Equal(1, winner.WinCount);
        Assert.Equal(1, loser.LoseCount);
        Assert.Equal(16, winner.ScoreDiff, 3);
    }

    [Fact]
    public async Task Should_CreateMissingPlayers_When_AResultUsesUnknownInitials()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(new CreateTournament(Any.Tournament().Name));
        var unknown = Enumerable.Range(0, 4).Select(_ => Any.Initials()).Distinct().ToList();

        // Act - nobody was registered up front.
        await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([unknown[0], unknown[1]], 10),
            new MatchTeamInput([unknown[2], unknown[3]], 4),
        ]));

        // Assert
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));
        Assert.Equal(4, scoreboard.Count);
    }

    [Fact]
    public async Task Should_RecalculateEverything_When_APlayedMatchIsEdited()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);
        var matchId = Guid.NewGuid();

        await factory.SendAsync(new SaveMatch(tournamentId, matchId,
        [
            new MatchTeamInput([players[0], players[1]], 10),
            new MatchTeamInput([players[2], players[3]], 6),
        ]));

        // Act - the result was written down wrong, the other team won.
        await factory.SendAsync(new SaveMatch(tournamentId, matchId,
        [
            new MatchTeamInput([players[0], players[1]], 6),
            new MatchTeamInput([players[2], players[3]], 10),
        ]));

        // Assert - the first result leaves no trace.
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));
        var firstTeam = scoreboard.Single(row => row.Initials == players[0]);
        var secondTeam = scoreboard.Single(row => row.Initials == players[2]);

        Assert.Equal(1184, firstTeam.Score, 3);
        Assert.Equal(1216, secondTeam.Score, 3);
        Assert.Equal(1, firstTeam.MatchCount);
        Assert.Equal(0, firstTeam.WinCount);
        Assert.Equal(1, secondTeam.WinCount);
    }

    [Fact]
    public async Task Should_RemoveTheResult_When_AMatchIsCancelled()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);
        var matchId = Guid.NewGuid();
        await factory.SendAsync(new SaveMatch(tournamentId, matchId,
        [
            new MatchTeamInput([players[0], players[1]], 10),
            new MatchTeamInput([players[2], players[3]], 6),
        ]));

        // Act
        await factory.SendAsync(new CancelMatch(tournamentId, matchId));

        // Assert
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));
        Assert.All(scoreboard, row => Assert.Equal(1200, row.Score, 3));
        Assert.All(scoreboard, row => Assert.Equal(0, row.MatchCount));
    }

    [Fact]
    public async Task Should_ReuseTheTeam_When_TheSamePlayersPlayTogetherAgain()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);

        // Act
        await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([players[0], players[1]], 10),
            new MatchTeamInput([players[2], players[3]], 6),
        ]));
        await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([players[1], players[0]], 7),
            new MatchTeamInput([players[3], players[2]], 10),
        ]));

        // Assert - the order of the initials does not create new teams.
        var teams = await factory.QueryAsync(db =>
            db.TournamentTeams.Where(team => team.TournamentId == tournamentId).ToListAsync());

        Assert.Equal(2, teams.Count);
        Assert.Equal([1, 2], teams.Select(team => team.Number).OrderBy(number => number));
        Assert.All(teams, team => Assert.Equal($"Team {team.Number}", team.Name));
    }

    [Fact]
    public async Task Should_KeepTheMatchPlanned_When_ItIsSavedAsPlanned()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);

        // Act
        var matchId = await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([players[0], players[1]], 0),
            new MatchTeamInput([players[2], players[3]], 0),
        ], AsPlanned: true));

        // Assert
        var match = await factory.SendAsync(new GetMatch(tournamentId, matchId));
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));

        Assert.Equal(MatchState.Planned, match!.State);
        Assert.All(scoreboard, row => Assert.Equal(0, row.MatchCount));
    }

    [Fact]
    public async Task Should_RefuseTheResult_When_NobodyScored()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
            [
                new MatchTeamInput([players[0], players[1]], 0),
                new MatchTeamInput([players[2], players[3]], 0),
            ])));
    }

    [Fact]
    public async Task Should_RefuseTheMatch_When_APlayerIsOnBothTeams()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(2);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
            [
                new MatchTeamInput([players[0]], 10),
                new MatchTeamInput([players[0]], 8),
            ])));
    }

    [Fact]
    public async Task Should_NumberMatchesInSequence_When_MatchesAreCreated()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);

        // Act
        for (var round = 0; round < 3; round++)
        {
            await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
            [
                new MatchTeamInput([players[0], players[1]], 10),
                new MatchTeamInput([players[2], players[3]], round),
            ]));
        }

        // Assert
        var matches = await factory.SendAsync(new GetMatches(tournamentId));
        Assert.Equal([1, 2, 3], matches.Played.Select(match => match.Order).OrderBy(order => order));
    }

    [Fact]
    public async Task Should_PlanAGamePerPlayerPerRound_When_SeveralMatchesArePlanned()
    {
        // Arrange
        var (tournamentId, _) = await TournamentWithPlayersAsync(8);

        // Act
        var created = await factory.SendAsync(new PlanMatches(
            tournamentId, GamesPerPlayer: 2, FixedTeams: false, Seeding: SeedingType.Fair,
            RandomSeed: 42));

        // Assert - four teams give two matches per round, two rounds is four matches.
        var matches = await factory.SendAsync(new GetMatches(tournamentId));
        Assert.Equal(4, created);
        Assert.Equal(4, matches.Planned.Count);
        Assert.All(matches.Planned, match => Assert.Equal(2, match.Teams.Count));
        Assert.All(matches.Planned, match =>
            Assert.All(match.Teams, team => Assert.Equal(2, team.Players.Count)));
    }

    [Fact]
    public async Task Should_UseTheSeedTournament_When_PlanningWithOne()
    {
        // Arrange - the seed tournament is remembered on the tournament.
        var (seedId, players) = await TournamentWithPlayersAsync(4);
        var tournamentId = await factory.SendAsync(new CreateTournament(Any.Tournament().Name));
        foreach (var initials in players)
        {
            await factory.SendAsync(new AddPlayerToTournament(tournamentId, initials));
        }

        // Act
        await factory.SendAsync(new PlanMatches(
            tournamentId, GamesPerPlayer: 1, FixedTeams: false, Seeding: SeedingType.Equality,
            SeedTournamentId: seedId, RandomSeed: 1));

        // Assert
        var tournament = await factory.SendAsync(new GetTournament(tournamentId));
        var matches = await factory.SendAsync(new GetMatches(tournamentId));

        Assert.Equal(seedId, tournament!.SeedTournamentId);
        Assert.Single(matches.Planned);
    }

    [Fact]
    public async Task Should_TurnAPlannedMatchIntoAResult_When_TheResultIsSavedOnTheSameMatch()
    {
        // Arrange
        var (tournamentId, players) = await TournamentWithPlayersAsync(4);
        var matchId = await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([players[0], players[1]], 0),
            new MatchTeamInput([players[2], players[3]], 0),
        ], AsPlanned: true));

        // Act
        await factory.SendAsync(new SaveMatch(tournamentId, matchId,
        [
            new MatchTeamInput([players[0], players[1]], 10),
            new MatchTeamInput([players[2], players[3]], 8),
        ]));

        // Assert
        var matches = await factory.SendAsync(new GetMatches(tournamentId));
        Assert.Empty(matches.Planned);
        Assert.Single(matches.Played);
        Assert.Equal(matchId, matches.Played.Single().Id);
    }

    /// <summary>Creates a tournament with the wanted number of players and returns their initials.</summary>
    private async Task<(Guid TournamentId, List<string> Players)> TournamentWithPlayersAsync(int playerCount)
    {
        var tournamentId = await factory.SendAsync(
            new CreateTournament(Any.Tournament().Name, TeamSize: playerCount == 2 ? 1 : 2));

        var initials = new List<string>();
        while (initials.Count < playerCount)
        {
            var candidate = Any.Initials();
            if (!initials.Contains(candidate))
            {
                initials.Add(candidate);
            }
        }

        foreach (var player in initials)
        {
            await factory.SendAsync(new AddPlayerToTournament(tournamentId, player));
        }

        return (tournamentId, initials);
    }
}
