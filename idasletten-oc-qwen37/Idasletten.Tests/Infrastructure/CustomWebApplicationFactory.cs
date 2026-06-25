using Idasletten.Data;
using Idasletten.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            SeedData(db);
        });
    }

    private void SeedData(AppDbContext db)
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "TEST1",
            Name = "Test User 1",
            Email = "test1@example.com"
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "TEST2",
            Name = "Test User 2",
            Email = "test2@example.com"
        };

        db.Users.AddRange(user1, user2);
        db.SaveChanges();

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Test Tournament",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true
        };

        db.Tournaments.Add(tournament);
        db.SaveChanges();

        var player1 = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            TournamentId = tournament.Id,
            Score = 1500
        };

        var player2 = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            TournamentId = tournament.Id,
            Score = 1500
        };

        db.TournamentPlayers.AddRange(player1, player2);
        db.SaveChanges();
    }
}
