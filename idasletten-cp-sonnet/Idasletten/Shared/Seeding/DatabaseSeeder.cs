using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Users.Entities;
using Idasletten.Shared.Data;

namespace Idasletten.Shared.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _db;

    public DatabaseSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        if (_db.Users.Any())
            return;

        var users = new[]
        {
            new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Username = "ALK", Name = "Anders Larsen Kjær" },
            new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Username = "BNS", Name = "Bo Nielsen Sørensen" },
            new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Username = "CHR", Name = "Christian Hansen Rasmussen" },
            new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Username = "DBR", Name = "Dorte Bagger Riber" },
        };

        await _db.Users.AddRangeAsync(users);

        var tournament = new Tournament
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222201"),
            Name = "Valhalla Cup 2024",
            TeamSize = 2,
            PointsToWin = 5,
            ScoreSystem = ScoreSystem.Elo,
            IsPublic = true,
        };

        await _db.Tournaments.AddAsync(tournament);

        var players = users.Select((u, i) => new Features.Tournaments.Entities.TournamentPlayer
        {
            UserId = u.Id,
            TournamentId = tournament.Id,
            Score = 1000.0,
            Lives = 3,
        }).ToList();

        await _db.TournamentPlayers.AddRangeAsync(players);

        await _db.SaveChangesAsync();
    }
}
