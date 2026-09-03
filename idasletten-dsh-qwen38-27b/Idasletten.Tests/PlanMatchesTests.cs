using Idasletten.Features.Common;
using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Matches.Commands.PlanMatches;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class PlanMatchesTests : IAsyncLifetime
{
    private TestDb _db = null!;

    public async Task InitializeAsync() => _db = await TestDb.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private IMediator Mediator => _db.Services.GetRequiredService<IMediator>();

    /// <summary>10 players in a 1v1 tournament with distinct ratings (best → worst: A..J).</summary>
    private async Task<Guid> SeedRankedTournamentAsync()
    {
        var t = Any.Tournament(teamSize: 1);
        var players = new[] { "AAA", "BBB", "CCC", "DDD", "EEE", "FFF", "GGG", "HHH", "III", "JJJ" };
        var tuples = players.Select((initials, i) =>
        {
            var user = Any.User(initials);
            var p = Any.Player(user, t);
            p.Score = 1600 - i * 10; // AAA best … JJJ worst
            return (user, p);
        }).ToArray();
        await _db.AddTournamentAsync(t, tuples);
        return t.Id;
    }

    private async Task<Dictionary<string, string>> PlannedPairsAsync(Guid tournamentId)
    {
        var pairs = new Dictionary<string, string>();
        var matches = await _db.Db.TournamentMatches
            .Include(m => m.TeamSlots).ThenInclude(s => s.Team).ThenInclude(tm => tm.Players).ThenInclude(tp => tp.Player).ThenInclude(pl => pl.User)
            .Where(m => m.TournamentId == tournamentId && m.State == MatchState.Planned)
            .ToListAsync();
        foreach (var m in matches)
        {
            var names = m.TeamSlots
                .Select(s => string.Join("+", s.Team.Players.Select(tp => tp.Player.User.Username)))
                .OrderBy(n => n)
                .ToList();
            pairs[names[0]] = names[1];
        }
        return pairs;
    }

    [Fact]
    public async Task Should_PairTopHalfWithBottomHalf_When_FairSeeding()
    {
        // Arrange
        var id = await SeedRankedTournamentAsync();

        // Act
        var count = await Mediator.Send(new PlanMatchesCommand(id, null, false, 1, false, SeedingType.Fair));

        // Assert — spec example: 1+6, 2+7, 3+8, 4+9, 5+10
        Assert.Equal(5, count);
        var pairs = await PlannedPairsAsync(id);
        Assert.Equal("FFF", pairs["AAA"]);
        Assert.Equal("GGG", pairs["BBB"]);
        Assert.Equal("HHH", pairs["CCC"]);
        Assert.Equal("III", pairs["DDD"]);
        Assert.Equal("JJJ", pairs["EEE"]);
    }

    [Fact]
    public async Task Should_PairBestWithWorst_When_EqualitySeeding()
    {
        // Arrange
        var id = await SeedRankedTournamentAsync();

        // Act
        var count = await Mediator.Send(new PlanMatchesCommand(id, null, false, 1, false, SeedingType.Equality));

        // Assert — 1+10, 2+9, 3+8, 4+7, 5+6
        Assert.Equal(5, count);
        var pairs = await PlannedPairsAsync(id);
        Assert.Equal("JJJ", pairs["AAA"]);
        Assert.Equal("III", pairs["BBB"]);
        Assert.Equal("HHH", pairs["CCC"]);
        Assert.Equal("GGG", pairs["DDD"]);
        Assert.Equal("FFF", pairs["EEE"]);
    }

    [Fact]
    public async Task Should_PlanMultipleGames_When_GamesPerPlayerIsHigher()
    {
        // Arrange
        var id = await SeedRankedTournamentAsync();

        // Act — 2 games per player → 10 matches for 10 players in 1v1
        var count = await Mediator.Send(new PlanMatchesCommand(id, null, false, 2, false, SeedingType.Random));

        // Assert
        Assert.Equal(10, count);
        Assert.Equal(10, await _db.Db.TournamentMatches.CountAsync(m => m.TournamentId == id && m.State == MatchState.Planned));
    }

    [Fact]
    public async Task Should_Throw_When_TournamentHasParentRound()
    {
        // Arrange — child round cannot be seeded
        var parent = Any.Tournament();
        var child = Any.Tournament(parentTournamentId: parent.Id);
        await _db.AddTournamentAsync(parent);
        var thu = Any.User("THO");
        var lovi = Any.User("LOV");
        await _db.AddTournamentAsync(child,
            (thu, Any.Player(thu, child)),
            (lovi, Any.Player(lovi, child)));

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(
            new PlanMatchesCommand(child.Id, null, false, 1, false, SeedingType.Fair)));
    }

    [Fact]
    public async Task Should_Throw_When_NotEnoughPlayersForTwoTeams()
    {
        // Arrange
        var t = Any.Tournament(teamSize: 1);
        var user = Any.User("THO");
        await _db.AddTournamentAsync(t, (user, Any.Player(user, t)));

        // Act / Assert
        await Assert.ThrowsAsync<FeatureException>(() => Mediator.Send(
            new PlanMatchesCommand(t.Id, null, false, 1, false, SeedingType.Fair)));
    }
}
