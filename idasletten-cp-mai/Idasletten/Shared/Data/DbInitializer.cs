using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IWebHostEnvironment env)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        await SeedTestUserAsync(userManager, config);

        if (env.IsDevelopment() && !await db.Tournaments.AnyAsync())
        {
            await SeedDemoDataAsync(db, userManager);
        }
    }

    private static async Task SeedTestUserAsync(UserManager<AppUser> userManager, IConfiguration config)
    {
        var email = config["TestUser__Email"];
        var password = config["TestUser__Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Username = "TST",
            Name = "Test User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, password);
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db, UserManager<AppUser> userManager)
    {
        var users = new[] { "ODN", "THO", "KLA", "BJA", "MNI", "FRE", "LAR", "ANS" };
        var userIds = new Dictionary<string, Guid>();
        foreach (var u in users)
        {
            var existing = await userManager.FindByNameAsync(u);
            if (existing == null)
            {
                var user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = u,
                    Email = $"{u.ToLowerInvariant()}@example.com",
                    Username = u,
                    Name = u,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Password123!");
                userIds[u] = user.Id;
            }
            else
            {
                userIds[u] = existing.Id;
            }
        }

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Sommerturnering 2026",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true
        };
        db.Tournaments.Add(tournament);

        foreach (var kv in userIds)
        {
            db.TournamentPlayers.Add(new TournamentPlayer
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                UserId = kv.Value,
                Score = 1500,
                Lives = 3,
                TrueSkillMean = 25,
                TrueSkillStdDev = 25.0 / 3.0
            });
        }

        await db.SaveChangesAsync();
    }
}
