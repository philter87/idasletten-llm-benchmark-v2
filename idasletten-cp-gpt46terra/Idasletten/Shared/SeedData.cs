using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IdaslettenDbContext db, IConfiguration configuration)
    {
        var testEmail = configuration["TestUser:Email"];
        if (!string.IsNullOrWhiteSpace(testEmail) &&
            !await db.Users.AnyAsync(x => x.Email == testEmail))
        {
            db.Users.Add(new User
            {
                Username = "TEST",
                Name = "Test Viking",
                Email = testEmail
            });
            await db.SaveChangesAsync();
        }

        if (await db.Tournaments.AnyAsync())
            return;

        var players = new[]
        {
            new User { Username = "IDA", Name = "Ida" },
            new User { Username = "THR", Name = "Thor" },
            new User { Username = "FRE", Name = "Freja" },
            new User { Username = "LOK", Name = "Loke" }
        };
        var tournament = new Tournament
        {
            Name = "Valhalla Friday Cup",
            IsPublic = true,
            ScoreSystem = ScoreSystem.Elo
        };
        db.Users.AddRange(players);
        db.Tournaments.Add(tournament);
        foreach (var user in players)
            tournament.Players.Add(new TournamentPlayer { User = user, Lives = null });
        await db.SaveChangesAsync();
    }
}
