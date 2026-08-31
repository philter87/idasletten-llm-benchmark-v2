using Idasletten.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class SeedDiagnosticTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public SeedDiagnosticTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_ShowSeededPlayers_When_DatabaseIsSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vo = await db.Tournaments.FirstAsync(t => t.Name == "Valkyrior Open");
        var players = await db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == vo.Id)
            .Select(p => p.User.Username)
            .ToListAsync();
        // Assert — the seed creates all ten players
        Assert.Equal(10, players.Count);
        Assert.Equal(players.Count, players.Distinct().Count());
    }
}
