using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Tournaments.AnyAsync()) return;

        var users = new[]
        {
            new User { Id = Guid.NewGuid(), Username = "MJS", Name = "Magnus Jørgensen", Email = "mjs@mjolner.dk" },
            new User { Id = Guid.NewGuid(), Username = "PCH", Name = "Peter Hansen", Email = "pch@mjolner.dk" },
            new User { Id = Guid.NewGuid(), Username = "ASK", Name = "Anders Skov", Email = "ask@mjolner.dk" },
            new User { Id = Guid.NewGuid(), Username = "JBN", Name = "Julie Bendix", Email = "jbn@mjolner.dk" },
            new User { Id = Guid.NewGuid(), Username = "KLM", Name = "Kasper Lund", Email = "klm@mjolner.dk" },
            new User { Id = Guid.NewGuid(), Username = "TNS", Name = "Thomas Nilsson", Email = "tns@mjolner.dk" },
        };

        await db.Users.AddRangeAsync(users);

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Fredagsliga 2025",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            IsArchived = false,
        };

        await db.Tournaments.AddAsync(tournament);

        var players = users.Select(u => new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = u.Id,
            TournamentId = tournament.Id,
            Score = 1000,
            Lives = 3,
        }).ToList();

        await db.TournamentPlayers.AddRangeAsync(players);

        await db.SaveChangesAsync();
    }

    public static async Task SeedTestUserAsync(AppDbContext db, string email, string username = "TST")
    {
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Name = "Test User",
            Email = email,
        };

        await db.Users.AddAsync(testUser);
        await db.SaveChangesAsync();
    }
}
