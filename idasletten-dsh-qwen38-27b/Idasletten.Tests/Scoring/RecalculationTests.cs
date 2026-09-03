using Idasletten.Features.Matches.Commands.RecordMatchResult;
using Microsoft.Extensions.DependencyInjection;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests.Scoring;

public class RecalculationTests : IAsyncLifetime
{
    private TestDb _db = null!;

    public async Task InitializeAsync() => _db = await TestDb.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private IMediator Mediator => _db.Services.GetRequiredService<IMediator>();

    /// <summary>Edits of finished matches require an authenticated HttpContext.</summary>
    private void Authenticate()
    {
        var accessor = _db.Services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        accessor.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "tester")], "AppCookie"))
        };
    }

    private static List<TeamInput> Teams(params (string[] Players, int Goals)[] teams) =>
        teams.Select(t => new TeamInput { PlayerInitials = t.Players.ToList(), Goals = t.Goals }).ToList();

    [Fact]
    public async Task Should_MatchFullReplay_When_FinishedMatchIsEdited()
    {
        // Arrange — tournament with THO and LOV; two matches: THO wins, then LOV wins
        var tournament = Any.Tournament(ScoreSystem.Elo, teamSize: 1);
        var thu = Any.User("THO");
        var lovi = Any.User("LOV");
        await _db.AddTournamentAsync(tournament,
            (thu, Any.Player(thu, tournament)),
            (lovi, Any.Player(lovi, tournament)));
        foreach (var p in _db.Db.TournamentPlayers.Where(p => p.TournamentId == tournament.Id))
            p.Score = Idasletten.Scoring.EloScoring.BaseRating;
        await _db.Db.SaveChangesAsync();

        var m1 = await Mediator.Send(new RecordMatchResultCommand(tournament.Id, null,
            Teams((["THO"], 5), (["LOV"], 2))));
        await Mediator.Send(new RecordMatchResultCommand(tournament.Id, null,
            Teams((["LOV"], 5), (["THO"], 4))));
        Authenticate();

        // Act — flip match 1 so LOV won it instead
        await Mediator.Send(new RecordMatchResultCommand(tournament.Id, m1,
            Teams((["THO"], 0), (["LOV"], 5))));

        // Assert — final state must equal a from-scratch replay: LOV 2–0, THO 0–2
        var players = await _db.Db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id).ToListAsync();
        var tho = players.Single(p => p.User.Username == "THO");
        var lov = players.Single(p => p.User.Username == "LOV");

        Assert.Equal(0, tho.WinCount);
        Assert.Equal(2, tho.LoseCount);
        Assert.Equal(2, lov.WinCount);
        Assert.Equal(0, lov.LoseCount);
        Assert.Equal(10, lov.PointsWon);
        Assert.True(lov.Score > 1500, $"LOV should lead, got {lov.Score}");
        Assert.True(tho.Score < 1500, $"THO should trail, got {tho.Score}");
    }

    [Fact]
    public async Task Should_UpdateOnlyTheEditedMatch_When_ResultChanges()
    {
        // Arrange
        var tournament = Any.Tournament(ScoreSystem.Elo, teamSize: 1);
        var thu = Any.User("THO");
        var lovi = Any.User("LOV");
        await _db.AddTournamentAsync(tournament,
            (thu, Any.Player(thu, tournament)),
            (lovi, Any.Player(lovi, tournament)));
        foreach (var p in _db.Db.TournamentPlayers.Where(p => p.TournamentId == tournament.Id))
            p.Score = Idasletten.Scoring.EloScoring.BaseRating;
        await _db.Db.SaveChangesAsync();

        var m1 = await Mediator.Send(new RecordMatchResultCommand(tournament.Id, null,
            Teams((["THO"], 5), (["LOV"], 2))));
        Authenticate();

        // Act — same match, new result (LOV wins 1–0 instead)
        await Mediator.Send(new RecordMatchResultCommand(tournament.Id, m1,
            Teams((["THO"], 0), (["LOV"], 1))));

        // Assert — still exactly one match; points reflect the new result
        var players = await _db.Db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id).ToListAsync();
        Assert.Equal(1, _db.Db.TournamentMatches.Count(m => m.TournamentId == tournament.Id));
        Assert.Equal(1, players.Single(p => p.User.Username == "LOV").PointsWon);
        Assert.Equal(0, players.Single(p => p.User.Username == "THO").PointsWon);
    }
}
