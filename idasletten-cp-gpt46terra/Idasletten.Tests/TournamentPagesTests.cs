using System.Net;
using Idasletten.Features.Tournaments;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class TournamentPagesTests(IdaslettenWebApplicationFactory factory) : IClassFixture<IdaslettenWebApplicationFactory>
{
    [Fact]
    public async Task Should_ShowPublicTournament_When_RequestingHomePage()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Valhalla Friday Cup", body);
    }

    [Fact]
    public async Task Should_ShowSeededPlayers_When_RequestingTournamentDetail()
    {
        // Arrange
        var client = factory.CreateClient();
        var tournamentId = await GetSeedTournamentIdAsync();

        // Act
        var response = await client.GetAsync($"/tournaments/{tournamentId}");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("IDA", body);
        Assert.Contains("Stilling", body);
    }

    [Fact]
    public async Task Should_RecalculateScore_When_RecordingMatch()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Idasletten.Shared.IdaslettenDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var tournamentId = (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Tournaments)).Id;

        // Act
        await sender.Send(new SaveMatchCommand(
            tournamentId,
            null,
            [new[] { "IDA", "THR" }, new[] { "FRE", "LOK" }],
            [5, 3]));
        var tournament = await sender.Send(new GetTournamentQuery(tournamentId));

        // Assert
        Assert.NotNull(tournament);
        Assert.Single(tournament.Recent);
        Assert.Contains(tournament.Players, x => x.Initials == "IDA" && x.Won == 1 && x.Score > 1000);
        Assert.Equal(2, (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.TournamentTeams))
            .Select(x => x.Number).Distinct().Count());
    }

    private async Task<Guid> GetSeedTournamentIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Idasletten.Shared.IdaslettenDbContext>();
        return (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Tournaments)).Id;
    }
}
