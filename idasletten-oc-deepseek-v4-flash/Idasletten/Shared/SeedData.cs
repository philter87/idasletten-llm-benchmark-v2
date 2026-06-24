using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync())
            return;

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "TestUser",
            Initials = "TST",
            Name = "Test User",
            Email = Environment.GetEnvironmentVariable("TestUser__Email"),
            NormalizedEmail = Environment.GetEnvironmentVariable("TestUser__Email")?.ToUpperInvariant()
        };

        var password = Environment.GetEnvironmentVariable("TestUser__Password");
        if (!string.IsNullOrEmpty(password))
        {
            await userManager.CreateAsync(testUser, password);
        }

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Idasletten Championship",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true
        };
        context.Tournaments.Add(tournament);

        var players = new List<User>();
        var initials = new[] { ("OLA", "Ola Nordmann"), ("KAR", "Kari Hansen"), ("PER", "Per Olsen"), ("LIV", "Liv Jensen") };
        foreach (var (username, name) in initials)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = username,
                Initials = username,
                Name = name
            };
            players.Add(user);

            context.TournamentPlayers.Add(new TournamentPlayer
            {
                UserId = user.Id,
                TournamentId = tournament.Id,
                Score = 1000,
                Lives = 3
            });
        }
        context.Users.AddRange(players);
        await context.SaveChangesAsync();
    }
}
