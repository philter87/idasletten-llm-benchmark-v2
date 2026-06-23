using Idasletten.Data;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Idasletten.Tests;

public class MatchTests : IClassFixture<IdaslettenFactory>
{
    private readonly IdaslettenFactory _factory;
    public MatchTests(IdaslettenFactory factory) => _factory = factory;

    private static List<TeamInput> TwoTeams(string[] a, int ga, string[] b, int gb) => new()
    {
        new(a.ToList(), ga),
        new(b.ToList(), gb)
    };

    [Fact]
    public async Task Should_AutoCreateUsers_When_UnknownInitialsUsed()
    {
        // Arrange
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Elo, null, true));
        var thor = Any.Initials();
        var odin = Any.Initials();

        // Act
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null,
            TwoTeams(new[] { thor, odin }, 5, new[] { Any.Initials(), Any.Initials() }, 3)));

        // Assert
        await _factory.Query(async db =>
        {
            Assert.True(await db.Users.AnyAsync(u => u.NormalizedUserName == thor.ToUpperInvariant()));
            Assert.Equal(4, await db.TournamentPlayers.CountAsync(p => p.TournamentId == id));
        });
    }

    [Fact]
    public async Task Should_RaiseWinnerScoreAboveLoser_When_EloMatchRecorded()
    {
        // Arrange
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Elo, null, true));
        string winA = "WIA", winB = "WIB", loA = "LOA", loB = "LOB";

        // Act
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null,
            TwoTeams(new[] { winA, winB }, 5, new[] { loA, loB }, 1)));

        // Assert
        var detail = await _factory.Send(new GetTournamentDetailQuery(id));
        var winner = detail!.Scoreboard.First(r => r.Initials == winA);
        var loser = detail.Scoreboard.First(r => r.Initials == loA);
        Assert.True(winner.Score > 1000, $"winner {winner.Score} should rise above the 1000 baseline");
        Assert.True(loser.Score < 1000, $"loser {loser.Score} should drop below the 1000 baseline");
        Assert.Equal(1, winner.WinCount);
        Assert.Equal(1, loser.LoseCount);
    }

    [Fact]
    public async Task Should_RecalculateScores_When_CompletedMatchEdited()
    {
        // Arrange — record a match, capture the winner's score.
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Elo, null, true));
        var matchId = Guid.NewGuid();
        await _factory.Send(new CreateOrUpdateMatchCommand(id, matchId,
            TwoTeams(new[] { "AAA", "BBB" }, 5, new[] { "CCC", "DDD" }, 0)));

        // Act — flip the result the other way for the same match.
        await _factory.Send(new CreateOrUpdateMatchCommand(id, matchId,
            TwoTeams(new[] { "AAA", "BBB" }, 0, new[] { "CCC", "DDD" }, 5)));

        // Assert — AAA now lost, so should be below baseline, and there is still only one match.
        var detail = await _factory.Send(new GetTournamentDetailQuery(id));
        var aaa = detail!.Scoreboard.First(r => r.Initials == "AAA");
        Assert.True(aaa.Score < 1000);
        Assert.Equal(1, aaa.LoseCount);
        Assert.Equal(0, aaa.WinCount);
        await _factory.Query(async db =>
            Assert.Equal(1, await db.TournamentMatches.CountAsync(m => m.TournamentId == id)));
    }

    [Fact]
    public async Task Should_SaveAsPlanned_When_GoalsOmitted()
    {
        // Arrange
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Elo, null, true));

        // Act
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null, new()
        {
            new TeamInput(new() { "PPP", "QQQ" }, null),
            new TeamInput(new() { "RRR", "SSS" }, null)
        }));

        // Assert
        await _factory.Query(async db =>
        {
            var match = await db.TournamentMatches.SingleAsync(m => m.TournamentId == id);
            Assert.Equal(MatchState.Planned, match.State);
        });
    }
}
