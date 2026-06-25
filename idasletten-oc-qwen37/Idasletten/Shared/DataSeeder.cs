using Idasletten.Data;
using Idasletten.Models;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "ABC",
            Name = "Alice Brown Charlie",
            Email = "alice@example.com"
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "DEF",
            Name = "David Edward Frank",
            Email = "david@example.com"
        };

        var user3 = new User
        {
            Id = Guid.NewGuid(),
            Username = "GHI",
            Name = "George Henry Ivan",
            Email = "george@example.com"
        };

        var user4 = new User
        {
            Id = Guid.NewGuid(),
            Username = "JKL",
            Name = "John Kevin Larry",
            Email = "john@example.com"
        };

        db.Users.AddRange(user1, user2, user3, user4);
        await db.SaveChangesAsync();

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Viking Championship",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            IsArchived = false
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var player1 = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            TournamentId = tournament.Id,
            Score = 1500,
            MatchCount = 0,
            WinCount = 0,
            LoseCount = 0
        };

        var player2 = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            TournamentId = tournament.Id,
            Score = 1500,
            MatchCount = 0,
            WinCount = 0,
            LoseCount = 0
        };

        var player3 = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user3.Id,
            TournamentId = tournament.Id,
            Score = 1500,
            MatchCount = 0,
            WinCount = 0,
            LoseCount = 0
        };

        var player4 = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user4.Id,
            TournamentId = tournament.Id,
            Score = 1500,
            MatchCount = 0,
            WinCount = 0,
            LoseCount = 0
        };

        db.TournamentPlayers.AddRange(player1, player2, player3, player4);
        await db.SaveChangesAsync();
    }
}
