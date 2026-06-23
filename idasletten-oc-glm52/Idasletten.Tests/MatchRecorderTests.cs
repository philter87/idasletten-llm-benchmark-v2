using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using Idasletten.Tests.TestData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests;

public class MatchRecorderTests
{
    private static IdaslettenDbContext BuildDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<IdaslettenDbContext>().UseSqlite(conn).Options;
        var db = new IdaslettenDbContext(opts);
        db.Database.Migrate();
        return db;
    }

    private static async Task<(IdaslettenDbContext db, Tournament t, List<TournamentPlayer> players)> SeedTournamentAsync(ScoreSystem system, int players = 4)
    {
        var db = BuildDb();
        var t = new Tournament { Name = Any.TournamentName(), TeamSize = 2, PointsToWin = 5, ScoreSystem = system };
        db.Tournaments.Add(t);
        await db.SaveChangesAsync();
        var playersByInitials = Enumerable.Range(0, players)
            .Select(_ => new Features.Users.User { Username = Any.Initials(), Name = Any.Name() }).ToList();
        db.Users.AddRange(playersByInitials);
        await db.SaveChangesAsync();
        var scoring = new ScoringSystemSelector().For(t);
        var tps = playersByInitials.Select(u =>
        {
            var tp = new TournamentPlayer { UserId = u.Id, TournamentId = t.Id };
            scoring.Initialise(tp);
            return tp;
        }).ToList();
        db.TournamentPlayers.AddRange(tps);
        await db.SaveChangesAsync();
        return (db, t, tps);
    }

    private static async Task PlayMatchAsync(IdaslettenDbContext db, Tournament t, List<TournamentPlayer> tpA, List<TournamentPlayer> tpB, int goalsA, int goalsB)
    {
        var match = new TournamentMatch { TournamentId = t.Id, Order = 1, State = MatchState.Planned };
        var ta = new TournamentTeam { TournamentId = t.Id, Number = 1, Name = "Team 1" };
        var tb = new TournamentTeam { TournamentId = t.Id, Number = 2, Name = "Team 2" };
        foreach (var p in tpA) ta.Players.Add(p);
        foreach (var p in tpB) tb.Players.Add(p);
        match.Teams.Add(ta);
        match.Teams.Add(tb);
        db.TournamentTeams.AddRange(ta, tb);
        db.TournamentMatches.Add(match);
        await db.SaveChangesAsync();
        await new MatchRecorder(db).RecordAsync(match, new List<TournamentTeamMatchResult>
        {
            new() { TeamId = ta.Id, GoalsWon = goalsA, GoalsLost = goalsB },
            new() { TeamId = tb.Id, GoalsWon = goalsB, GoalsLost = goalsA },
        });
    }

    private static TournamentPlayer Find(IdaslettenDbContext db, Guid id) =>
        db.TournamentPlayers.First(p => p.Id == id);

    [Fact]
    public async Task Should_IncrementWinCountForWinners_When_WinCountScoring()
    {
        var (db, t, players) = await SeedTournamentAsync(ScoreSystem.WinCount);
        var teamA = players.Take(2).ToList();
        var teamB = players.Skip(2).Take(2).ToList();
        await PlayMatchAsync(db, t, teamA, teamB, 5, 3);

        Assert.Equal(1, Find(db, teamA[0].Id).WinCount);
        Assert.Equal(1, Find(db, teamA[1].Id).WinCount);
        Assert.Equal(1, Find(db, teamB[0].Id).LoseCount);
        Assert.Equal(1, Find(db, teamB[1].Id).LoseCount);
        Assert.True(Find(db, teamA[0].Id).Score >= 1);
    }

    [Fact]
    public async Task Should_DecrementLives_When_LivesScoring()
    {
        var (db, t, players) = await SeedTournamentAsync(ScoreSystem.Lives);
        var teamA = players.Take(2).ToList();
        var teamB = players.Skip(2).Take(2).ToList();
        await PlayMatchAsync(db, t, teamA, teamB, 5, 3);

        Assert.Equal(3, Find(db, teamA[0].Id).Lives);
        Assert.Equal(3, Find(db, teamA[1].Id).Lives);
        Assert.Equal(2, Find(db, teamB[0].Id).Lives);
        Assert.Equal(2, Find(db, teamB[1].Id).Lives);
    }

    [Fact]
    public async Task Should_ApplyEloDeltas_When_EloScoringAndTeamWins()
    {
        var (db, t, players) = await SeedTournamentAsync(ScoreSystem.Elo);
        var teamA = players.Take(2).ToList();
        var teamB = players.Skip(2).Take(2).ToList();
        var beforeA = Find(db, teamA[0].Id).Score;
        await PlayMatchAsync(db, t, teamA, teamB, 5, 3);

        Assert.True(Find(db, teamA[0].Id).Score > beforeA, "Winner's Elo score should increase after a win.");
        Assert.True(Find(db, teamB[0].Id).Score < beforeA, "Loser's Elo score should drop below the initial mean.");
    }

    [Fact]
    public async Task Should_ProduceConservativeTrueSkillScore_When_TrueSkillScoring()
    {
        var (db, t, players) = await SeedTournamentAsync(ScoreSystem.TrueSkill);
        var teamA = players.Take(2).ToList();
        var teamB = players.Skip(2).Take(2).ToList();
        await PlayMatchAsync(db, t, teamA, teamB, 5, 3);

        Assert.True(Find(db, teamA[0].Id).Score > Find(db, teamB[0].Id).Score,
            $"Winner's conservative TrueSkill ({Find(db, teamA[0].Id).Score}) should exceed loser's ({Find(db, teamB[0].Id).Score})");
    }
}