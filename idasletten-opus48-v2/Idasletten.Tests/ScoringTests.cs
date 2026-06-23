using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Domain;
using Xunit;

namespace Idasletten.Tests;

public class ScoringTests : IClassFixture<IdaslettenFactory>
{
    private readonly IdaslettenFactory _factory;
    public ScoringTests(IdaslettenFactory factory) => _factory = factory;

    private static List<TeamInput> TwoTeams(string[] a, int ga, string[] b, int gb) => new()
    {
        new(a.ToList(), ga),
        new(b.ToList(), gb)
    };

    [Fact]
    public async Task Should_LoseOneLife_When_LivesMatchLost()
    {
        // Arrange
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.Lives, null, true));

        // Act — LSA/LSB lose once.
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null,
            TwoTeams(new[] { "WNA", "WNB" }, 5, new[] { "LSA", "LSB" }, 2)));

        // Assert — losers down to 2 lives, winners still at 3.
        var detail = await _factory.Send(new GetTournamentDetailQuery(id));
        Assert.Equal(2, detail!.Scoreboard.First(r => r.Initials == "LSA").Lives);
        Assert.Equal(3, detail.Scoreboard.First(r => r.Initials == "WNA").Lives);
    }

    [Fact]
    public async Task Should_RankByWins_When_WinCountSystem()
    {
        // Arrange
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 5, ScoreSystem.WinCount, null, true));

        // Act — TOP/TPB win twice; everyone else less.
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null,
            TwoTeams(new[] { "TOP", "TPB" }, 5, new[] { "MID", "MDB" }, 1)));
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null,
            TwoTeams(new[] { "TOP", "TPB" }, 5, new[] { "LOW", "LWB" }, 0)));

        // Assert — scoreboard is sorted by score (== wins); TOP leads with 2.
        var detail = await _factory.Send(new GetTournamentDetailQuery(id));
        var top = detail!.Scoreboard.First();
        Assert.Equal("TOP", top.Initials);
        Assert.Equal(2, top.Score);
        Assert.Equal(2, top.WinCount);
    }

    [Fact]
    public async Task Should_ProduceFiniteScores_When_TrueSkillMatchRecorded()
    {
        // Arrange
        var id = await _factory.Send(new CreateTournamentCommand(
            Any.String(), 2, 10, ScoreSystem.TrueSkill, null, true));

        // Act
        await _factory.Send(new CreateOrUpdateMatchCommand(id, null,
            TwoTeams(new[] { "TSA", "TSB" }, 10, new[] { "TSC", "TSD" }, 4)));

        // Assert — winner ends up ranked above loser with real numbers.
        var detail = await _factory.Send(new GetTournamentDetailQuery(id));
        var winner = detail!.Scoreboard.First(r => r.Initials == "TSA");
        var loser = detail.Scoreboard.First(r => r.Initials == "TSC");
        Assert.False(double.IsNaN(winner.Score));
        Assert.True(winner.Score > loser.Score);
    }
}
