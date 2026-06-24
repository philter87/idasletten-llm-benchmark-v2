using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Tournaments.AnyAsync())
            return;

        // Seed users
        var users = new[]
        {
            new User { Username = "PCH", Name = "Peder Christensen", Email = "pch@mjolner.dk" },
            new User { Username = "MKR", Name = "Mikkel Krage", Email = "mkr@mjolner.dk" },
            new User { Username = "JLN", Name = "Jan Larsen", Email = "jln@mjolner.dk" },
            new User { Username = "ABN", Name = "Anders Bonde", Email = "abn@mjolner.dk" },
            new User { Username = "TSM", Name = "Thomas Smed", Email = "tsm@mjolner.dk" },
            new User { Username = "KBJ", Name = "Kasper Bjerg", Email = "kbj@mjolner.dk" },
        };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // Seed a tournament
        var tournament = new Tournament
        {
            Name = "Idasletten Championship 2024",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            IsArchived = false
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        // Add players to tournament
        var players = users.Select(u => new TournamentPlayer
        {
            UserId = u.Id,
            TournamentId = tournament.Id,
            Score = 1000,
            Lives = 3
        }).ToList();
        db.TournamentPlayers.AddRange(players);
        await db.SaveChangesAsync();

        // Seed a past tournament
        var pastTournament = new Tournament
        {
            Name = "Valhalla Open 2023",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.WinCount,
            IsPublic = true,
            IsArchived = true
        };
        db.Tournaments.Add(pastTournament);
        await db.SaveChangesAsync();
    }

    public static async Task SeedTestUserAsync(AppDbContext db, string username, string name, string? email)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToUpperInvariant());
        if (existing == null)
        {
            db.Users.Add(new User
            {
                Username = username.ToUpperInvariant(),
                Name = name,
                Email = email
            });
            await db.SaveChangesAsync();
        }
    }
}
