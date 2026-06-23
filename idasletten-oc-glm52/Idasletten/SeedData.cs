using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Idasletten.Shared;
using Idasletten.Shared.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Idasletten;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<IdaslettenDbContext>();
        if (await db.Tournaments.AnyAsync()) return;
        if (await db.Users.AnyAsync()) return;

        var testUser = new User { Username = "TST", Name = "Test User", Email = "test@example.com" };
        db.Users.Add(testUser);

        var rnd = new Random(42);
        string[] initials = ["PHI", "JNK", "ODN", "THR", "BFR", "KLD", "MJS", "VAR", "AVR", "LTO"];
        var users = initials.Select(i => new User { Username = i, Name = $"Player {i}" }).ToList();
        db.Users.AddRange(users);

        var tournament = new Tournament
        {
            Name = "Ragnarok Series — Round 1",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.WinCount,
            IsPublic = true,
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var selector = new ScoringSystemSelector();
        var scoring = selector.For(tournament);
        var players = users.Select(u =>
        {
            var tp = new TournamentPlayer { UserId = u.Id, TournamentId = tournament.Id };
            scoring.Initialise(tp);
            return tp;
        }).ToList();
        db.TournamentPlayers.AddRange(players);
        await db.SaveChangesAsync();

        // Play a few sample matches so the scoreboard is not empty.
        var livePlayers = await db.TournamentPlayers.Include(p => p.User).ToListAsync();
        var byInitial = livePlayers.ToDictionary(p => p.User.Username);
        for (int m = 0; m < 4; m++)
        {
            var matchTeams = BuildRandomTeams(tournament, livePlayers, rnd);
            var match = new TournamentMatch { TournamentId = tournament.Id, Order = m + 1, State = MatchState.Planned };
            foreach (var team in matchTeams) match.Teams.Add(team);
            foreach (var t in matchTeams) t.TournamentId = tournament.Id;
            db.TournamentTeams.AddRange(matchTeams);
            db.TournamentMatches.Add(match);
            await db.SaveChangesAsync();

            var ga = rnd.Next(0, 6);
            var gb = rnd.Next(0, 6);
            if (ga == gb) gb = (gb + 1) % 6;
            var results = new List<TournamentTeamMatchResult>
            {
                new() { TeamId = matchTeams[0].Id, GoalsWon = ga, GoalsLost = gb },
                new() { TeamId = matchTeams[1].Id, GoalsWon = gb, GoalsLost = ga },
            };
            var recorder = new MatchRecorder(db);
            await recorder.RecordAsync(match, results);
        }
    }

    private static List<TournamentTeam> BuildRandomTeams(Tournament t, List<TournamentPlayer> pool, Random rnd)
    {
        var shuffled = pool.OrderBy(_ => rnd.Next()).Take(4).ToList();
        var teams = new List<TournamentTeam>();
        for (int i = 0; i < 2; i++)
        {
            var team = new TournamentTeam { TournamentId = t.Id, Number = i + 1, Name = $"Team {i + 1}" };
            foreach (var p in shuffled.Skip(i * 2).Take(2)) team.Players.Add(p);
            teams.Add(team);
        }
        return teams;
    }
}