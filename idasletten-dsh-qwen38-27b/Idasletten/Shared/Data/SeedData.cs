using Idasletten.Auth;
using Idasletten.Models;
using Idasletten.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Data;

/// <summary>
/// Demo data for local (in-memory) runs and integration tests. Creates users,
/// tournaments (all four score systems), players, finished and planned matches,
/// and — when TestUser:Email/Password are configured — the seeded test user.
/// Idempotent: does nothing when users already exist.
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var scoring = services.GetRequiredService<ScoringEngine>();
        var testUser = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestUserOptions>>().Value;

        if (await db.Users.AnyAsync())
            return;

        // ---------- users ----------
        var users = new (User U, string Initials)[]
        {
            (new User { Username = "THO", Name = "Thor Odinson", Email = "thor@mjolner.no" }, "THO"),
            (new User { Username = "LOV", Name = "Loki Laufeyson", Email = "loki@mjolner.no" }, "LOV"),
            (new User { Username = "ODF", Name = "Odin Borson", Email = "odin@mjolner.no" }, "ODF"),
            (new User { Username = "FRE", Name = "Freya Disdottir", Email = "freya@mjolner.no" }, "FRE"),
            (new User { Username = "BAL", Name = "Baldur Bragi", Email = "baldur@mjolner.no" }, "BAL"),
            (new User { Username = "HEO", Name = "Heimdall Allgard" }, "HEO"),
            (new User { Username = "SIG", Name = "Sif Jansdotter", Email = "sif@mjolner.no" }, "SIG"),
            (new User { Username = "TYR", Name = "Tyr Asgardian" }, "TYR"),
            (new User { Username = "BOD", Name = "Bragi Oden", Email = "bragi@mjolner.no" }, "BOD"),
            (new User { Username = "IDU", Name = "Idun Skadi", Email = "idun@mjolner.no" }, "IDU"),
        };
        db.Users.AddRange(users.Select(x => x.U));

        // ---------- test user (only when configured) ----------
        if (testUser.Enabled)
        {
            var tu = new User
            {
                Username = "TST",
                Name = "Test Viking",
                Email = testUser.Email,
                PasswordHash = PasswordHasher.Hash(testUser.Password!)
            };
            db.Users.Add(tu);
        }

        // ---------- tournaments ----------
        var valkyrior = new Tournament
        {
            Name = "Valkyrior Open",
            TeamSize = 1,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        var ragnarok = new Tournament
        {
            Name = "Ragnarok Cup",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.TrueSkill,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };
        var jotunheim = new Tournament
        {
            Name = "Jotunheim League",
            TeamSize = 1,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Lives,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var oldGods = new Tournament
        {
            Name = "Old Gods Invitational",
            TeamSize = 1,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.WinCount,
            IsPublic = true,
            IsArchived = true,
            CreatedAt = DateTime.UtcNow.AddDays(-90)
        };
        db.Tournaments.AddRange(valkyrior, ragnarok, jotunheim, oldGods);
        await db.SaveChangesAsync();

        // ---------- players ----------
        void AddPlayers(Tournament t, params string[] initials)
        {
            foreach (var i in initials)
            {
                var u = users.First(x => x.Initials == i).U;
                db.TournamentPlayers.Add(new TournamentPlayer { Tournament = t, User = u });
            }
        }
        AddPlayers(valkyrior, "THO", "LOV", "ODF", "FRE", "BAL", "HEO", "SIG", "TYR", "BOD", "IDU");
        AddPlayers(ragnarok, "THO", "LOV", "ODF", "FRE", "BAL", "HEO", "SIG", "TYR");
        AddPlayers(jotunheim, "BAL", "HEO", "SIG", "TYR", "BOD", "IDU");
        AddPlayers(oldGods, "THO", "ODF", "BAL", "HEO", "SIG", "TYR");
        await db.SaveChangesAsync();

        // ---------- finished matches ----------
        // Valkyrior Open (Elo, 1v1)
        await SeedMatchAsync(db, valkyrior, "THO", "LOV", 5, 3);
        await SeedMatchAsync(db, valkyrior, "ODF", "FRE", 2, 5);
        await SeedMatchAsync(db, valkyrior, "THO", "ODF", 5, 1);
        await SeedMatchAsync(db, valkyrior, "BAL", "HEO", 5, 4);
        await SeedMatchAsync(db, valkyrior, "SIG", "TYR", 0, 5);
        await SeedMatchAsync(db, valkyrior, "BOD", "IDU", 3, 5);
        await SeedMatchAsync(db, valkyrior, "THO", "BAL", 5, 2);
        await SeedMatchAsync(db, valkyrior, "ODF", "SIG", 5, 0);

        // Ragnarok Cup (TrueSkill, 2v2)
        await SeedMatchAsync(db, ragnarok, new[] { "THO", "LOV" }, new[] { "ODF", "FRE" }, 5, 4);
        await SeedMatchAsync(db, ragnarok, new[] { "BAL", "HEO" }, new[] { "SIG", "TYR" }, 1, 5);
        await SeedMatchAsync(db, ragnarok, new[] { "THO", "BAL" }, new[] { "LOV", "HEO" }, 5, 5);
        await SeedMatchAsync(db, ragnarok, new[] { "ODF", "SIG" }, new[] { "FRE", "TYR" }, 5, 2);

        // Jotunheim League (Lives, 1v1)
        await SeedMatchAsync(db, jotunheim, "BAL", "HEO", 5, 3);
        await SeedMatchAsync(db, jotunheim, "SIG", "TYR", 5, 4);
        await SeedMatchAsync(db, jotunheim, "BOD", "IDU", 5, 1);
        await SeedMatchAsync(db, jotunheim, "BAL", "SIG", 4, 5);
        await SeedMatchAsync(db, jotunheim, "HEO", "TYR", 5, 0);

        // Old Gods Invitational (WinCount, 1v1, archived)
        await SeedMatchAsync(db, oldGods, "THO", "ODF", 5, 3);
        await SeedMatchAsync(db, oldGods, "BAL", "HEO", 5, 2);
        await SeedMatchAsync(db, oldGods, "SIG", "TYR", 5, 4);
        await SeedMatchAsync(db, oldGods, "THO", "BAL", 5, 3);
        await SeedMatchAsync(db, oldGods, "ODF", "SIG", 2, 5);

        // ---------- planned matches ----------
        await SeedPlannedAsync(db, valkyrior, "FRE", "IDU");
        await SeedPlannedAsync(db, valkyrior, "HEO", "BOD");
        await SeedPlannedAsync(db, valkyrior, "TYR", "SIG");

        // ---------- round 2 (child of Valkyrior Open) ----------
        var round2 = new Tournament
        {
            Name = "Valkyrior Open — Round 2",
            ParentTournamentId = valkyrior.Id,
            RoundNumber = 2,
            TeamSize = 1,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Tournaments.Add(round2);
        await db.SaveChangesAsync();
        AddPlayers(round2, "THO", "ODF", "FRE", "BAL");
        await db.SaveChangesAsync();
        await SeedPlannedAsync(db, round2, "THO", "ODF");

        // ---------- compute all scores by replay ----------
        foreach (var t in new[] { valkyrior, ragnarok, jotunheim, oldGods, round2 })
            await scoring.RecalculateTournamentAsync(db, t);
    }

    private static async Task SeedMatchAsync(AppDbContext db, Tournament t, string a, string b, int goalsA, int goalsB)
    {
        await SeedMatchAsync(db, t, new[] { a }, new[] { b }, goalsA, goalsB);
    }

    private static async Task SeedMatchAsync(AppDbContext db, Tournament t, string[] teamA, string[] teamB, int goalsA, int goalsB)
    {
        var match = new TournamentMatch
        {
            Tournament = t,
            Order = await NextOrderAsync(db, t.Id),
            State = MatchState.Done
        };
        db.TournamentMatches.Add(match);
        await db.SaveChangesAsync();

        var teamAEntity = await SeedTeamAsync(db, t, 1, teamA, match);
        var teamBEntity = await SeedTeamAsync(db, t, 2, teamB, match);

        db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
        {
            Match = match,
            TournamentId = t.Id,
            TeamId = teamAEntity.Id,
            GoalsWon = goalsA,
            GoalsLost = goalsB
        });
        db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
        {
            Match = match,
            TournamentId = t.Id,
            TeamId = teamBEntity.Id,
            GoalsWon = goalsB,
            GoalsLost = goalsA
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedPlannedAsync(AppDbContext db, Tournament t, string a, string b)
    {
        var match = new TournamentMatch
        {
            Tournament = t,
            Order = await NextOrderAsync(db, t.Id),
            State = MatchState.Planned
        };
        db.TournamentMatches.Add(match);
        await db.SaveChangesAsync();
        await SeedTeamAsync(db, t, 1, new[] { a }, match);
        await SeedTeamAsync(db, t, 2, new[] { b }, match);
    }

    private static async Task<TournamentTeam> SeedTeamAsync(AppDbContext db, Tournament t, int number, string[] initials, TournamentMatch match)
    {
        var team = new TournamentTeam { Tournament = t, Number = number, Name = $"Team {number}" };
        db.TournamentTeams.Add(team);
        await db.SaveChangesAsync();
        foreach (var i in initials)
        {
            var player = db.TournamentPlayers.First(p => p.TournamentId == t.Id && p.User.Username == i);
            db.TeamPlayers.Add(new TeamPlayer { Team = team, Player = player });
        }
        db.MatchTeams.Add(new MatchTeam { Match = match, Team = team });
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task<int> NextOrderAsync(AppDbContext db, Guid tournamentId)
    {
        var max = await db.TournamentMatches.Where(m => m.TournamentId == tournamentId).Select(m => (int?)m.Order).MaxAsync();
        return (max ?? 0) + 1;
    }
}
