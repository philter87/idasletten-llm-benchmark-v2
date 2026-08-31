using Idasletten.Features.Common;
using Idasletten.Features.Matches.Commands.PlanMatch;
using Idasletten.Features.Matches.Commands.RecordMatchResult;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class MatchCommandTests : IAsyncLifetime
{
    private TestDb _db = null!;

    public async Task InitializeAsync() => _db = await TestDb.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private IMediator Mediator => _db.Services.GetRequiredService<IMediator>();

    private static List<TeamInput> Teams(params (string[] Players, int Goals)[] teams) =>
        teams.Select(t => new TeamInput { PlayerInitials = t.Players.ToList(), Goals = t.Goals }).ToList();

    /// <summary>Tournament with THO and LOV (distinct, persisted user instances).</summary>
    private async Task<Guid> TwoPlayerTournamentAsync(ScoreSystem system = ScoreSystem.Elo, bool archived = false)
    {
        var t = Any.Tournament(system, isArchived: archived);
        var thu = Any.User("THO");
        var lovi = Any.User("LOV");
        await _db.AddTournamentAsync(t, (thu, Any.Player(thu, t)), (lovi, Any.Player(lovi, t)));
        return t.Id;
    }

    [Fact]
    public async Task Should_RecordMatch_When_AnonymousUserRecordsNewResult()
    {
        // Arrange — no HttpContext at all: recording a NEW match must not require login
        var t = await TwoPlayerTournamentAsync();

        // Act
        var matchId = await Mediator.Send(new RecordMatchResultCommand(t, null,
            Teams((["THO"], 5), (["LOV"], 3))));

        // Assert
        var match = await _db.Db.TournamentMatches.SingleAsync(m => m.Id == matchId);
        Assert.Equal(MatchState.Done, match.State);
    }

    [Fact]
    public async Task Should_Throw_When_TeamHasWrongPlayerCount()
    {
        // Arrange
        var t = await TwoPlayerTournamentAsync();

        // Act / Assert — team size 1 but two players offered
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new RecordMatchResultCommand(t, null,
            Teams((["THO", "LOV"], 0), (["THO"], 0)))));
    }

    [Fact]
    public async Task Should_Throw_When_SamePlayerOnBothTeams()
    {
        // Arrange
        var t = await TwoPlayerTournamentAsync();

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new RecordMatchResultCommand(t, null,
            Teams((["THO"], 0), (["THO"], 0)))));
    }

    [Fact]
    public async Task Should_CompletePlannedMatch_When_ResultIsRecorded()
    {
        // Arrange
        var t = await TwoPlayerTournamentAsync();
        var plannedId = await Mediator.Send(new PlanMatchCommand(t,
            new List<IReadOnlyList<string>> { new List<string> { "THO" }, new List<string> { "LOV" } }));

        // Act
        var matchId = await Mediator.Send(new RecordMatchResultCommand(t, plannedId,
            Teams((["THO"], 5), (["LOV"], 1))));

        // Assert — same match, now Done
        Assert.Equal(plannedId, matchId);
        var match = await _db.Db.TournamentMatches.SingleAsync(m => m.Id == matchId);
        Assert.Equal(MatchState.Done, match.State);
    }

    [Fact]
    public async Task Should_Throw_When_EditingDoneMatchWithoutLogin()
    {
        // Arrange
        var t = await TwoPlayerTournamentAsync();
        var matchId = await Mediator.Send(new RecordMatchResultCommand(t, null,
            Teams((["THO"], 5), (["LOV"], 2))));

        // Act / Assert — no authenticated HttpContext → editing a Done match is refused
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new RecordMatchResultCommand(t, matchId,
            Teams((["THO"], 0), (["LOV"], 5)))));
    }

    [Fact]
    public async Task Should_AllowEditingDoneMatch_When_UserIsAuthenticated()
    {
        // Arrange
        var t = await TwoPlayerTournamentAsync();
        var matchId = await Mediator.Send(new RecordMatchResultCommand(t, null,
            Teams((["THO"], 5), (["LOV"], 2))));
        var accessor = _db.Services.GetRequiredService<IHttpContextAccessor>();
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "tester")],
                "AppCookie"));
        accessor.HttpContext = new DefaultHttpContext { User = user };

        // Act
        await Mediator.Send(new RecordMatchResultCommand(t, matchId,
            Teams((["THO"], 0), (["LOV"], 5))));

        // Assert — LOV now leads
        var players = await _db.Db.TournamentPlayers.Include(p => p.User)
            .Where(p => p.TournamentId == t).ToListAsync();
        var lov = players.Single(p => p.User.Username == "LOV");
        Assert.Equal(5, lov.PointsWon);
    }

    [Fact]
    public async Task Should_Throw_When_PlayerHasZeroLives()
    {
        // Arrange — Lives tournament; THO has lost all lives
        var t = Any.Tournament(ScoreSystem.Lives);
        var thu = Any.User("THO");
        var lovi = Any.User("LOV");
        var thop = Any.Player(thu, t);
        thop.Lives = 0; thop.Score = 0;
        await _db.AddTournamentAsync(t,
            (thu, thop),
            (lovi, Any.Player(lovi, t)));

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new RecordMatchResultCommand(t.Id, null,
            Teams((["THO"], 0), (["LOV"], 0)))));
    }

    [Fact]
    public async Task Should_Throw_When_TournamentIsArchived()
    {
        // Arrange
        var t = await TwoPlayerTournamentAsync(archived: true);

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(new RecordMatchResultCommand(t, null,
            Teams((["THO"], 5), (["LOV"], 0)))));
    }
}
