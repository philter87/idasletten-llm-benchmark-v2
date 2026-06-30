using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands.CreatePlannedMatch;
using Idasletten.Features.Matches.Commands.PlanMultipleMatches;
using Idasletten.Features.Matches.Commands.SaveMatch;
using Idasletten.Features.Matches.Queries.GetMatch;
using Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;
using Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Tests.TestData;
using MediatR;

namespace Idasletten.Tests.Features.Matches;

public class MatchFeatureTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Should_LeaveMatchPlanned_When_RecordResultIsFalse()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId));
        var teamA = Any.Username();
        var teamB = Any.Username();

        // Act
        await sender.Send(new SaveMatchCommand(
            matchId, tournamentId, [new TeamInput([teamA], 0), new TeamInput([teamB], 0)], RecordResult: false));

        // Assert
        var match = await sender.Send(new GetMatchQuery(matchId));
        Assert.Equal(MatchState.Planned, match!.State);
    }

    [Fact]
    public async Task Should_UpdatePlayerScores_When_ResultIsRecorded()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId));
        var winner = Any.Username();
        var loser = Any.Username();

        // Act
        await sender.Send(new SaveMatchCommand(
            matchId, tournamentId, [new TeamInput([winner], 5), new TeamInput([loser], 2)], RecordResult: true));

        // Assert
        var players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        Assert.True(players.Single(p => p.Username == winner).Score > 1200);
        Assert.True(players.Single(p => p.Username == loser).Score < 1200);
        Assert.Equal(1, players.Single(p => p.Username == winner).WinCount);
        Assert.Equal(1, players.Single(p => p.Username == loser).LoseCount);
    }

    [Fact]
    public async Task Should_RecalculateScores_When_DoneMatchIsEdited()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.WinCount, null, true));
        var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId));
        var playerA = Any.Username();
        var playerB = Any.Username();
        await sender.Send(new SaveMatchCommand(
            matchId, tournamentId, [new TeamInput([playerA], 5), new TeamInput([playerB], 2)], RecordResult: true));

        // Act: flip the result on the same (now Done) match.
        await sender.Send(new SaveMatchCommand(
            matchId, tournamentId, [new TeamInput([playerA], 2), new TeamInput([playerB], 5)], RecordResult: true));

        // Assert
        var players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        Assert.Equal(0, players.Single(p => p.Username == playerA).WinCount);
        Assert.Equal(1, players.Single(p => p.Username == playerB).WinCount);
    }

    [Fact]
    public async Task Should_AutoCreatePlayers_When_InitialsHaveNotBeenUsedBefore()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId));
        var brandNewUsername = Any.Username();

        // Act
        await sender.Send(new SaveMatchCommand(
            matchId, tournamentId, [new TeamInput([brandNewUsername], 5), new TeamInput([Any.Username()], 1)], RecordResult: true));

        // Assert
        var players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        Assert.Contains(players, p => p.Username == brandNewUsername);
    }

    [Fact]
    public async Task Should_CreatePlannedMatches_When_PlanningMultipleMatchesForExistingPlayers()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tournamentId = await sender.Send(new CreateTournamentCommand(Any.Word(), 2, 5, ScoreSystem.Elo, null, true));
        foreach (var _ in Enumerable.Range(0, 4))
        {
            await sender.Send(new AddPlayerToTournamentCommand(tournamentId, Any.Username()));
        }

        // Act
        var matchIds = await sender.Send(new PlanMultipleMatchesCommand(
            tournamentId, GamesPerPlayer: 1, FixedTeams: false, SeedingType.Random, SeedTournamentId: null));

        // Assert
        Assert.Single(matchIds);
        var match = await sender.Send(new GetMatchQuery(matchIds[0]));
        Assert.Equal(2, match!.Teams.Count);
        Assert.All(match.Teams, t => Assert.Equal(2, t.PlayerUsernames.Count));
    }
}
