using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Tests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features;

public class TournamentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Should_CreateAndRetrieveTournament()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Test Tournament",
            TeamSize = 2,
            PointsToWin = 5
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var result = await mediator.Send(new GetTournamentByIdQuery(tournament.Id));

        Assert.NotNull(result);
        Assert.Equal("Test Tournament", result.Name);
    }
}
