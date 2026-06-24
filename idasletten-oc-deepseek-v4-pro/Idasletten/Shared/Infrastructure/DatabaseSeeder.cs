using Idasletten.Shared.Entities;

namespace Idasletten.Shared.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var users = new[]
        {
            new User { Id = Guid.NewGuid(), Username = "RAG", Name = "Ragnar Lothbrok", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "LAG", Name = "Lagertha", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "BJO", Name = "Bjorn Ironside", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "FLO", Name = "Floki", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "ROL", Name = "Rollo", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "IVR", Name = "Ivar the Boneless", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "UBB", Name = "Ubbe", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "HVK", Name = "Hvitserk", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "SIG", Name = "Sigurd Snake-in-the-Eye", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "TOR", Name = "Torvi", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "HAR", Name = "Harald Finehair", CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Username = "HAL", Name = "Halfdan the Black", CreatedAt = DateTime.UtcNow },
        };

        db.Users.AddRange(users);

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Ragnarök Qualifier",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Tournaments.Add(tournament);

        foreach (var user in users)
        {
            db.TournamentPlayers.Add(new TournamentPlayer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TournamentId = tournament.Id,
                Score = 1000
            });
        }

        await db.SaveChangesAsync();
    }
}
