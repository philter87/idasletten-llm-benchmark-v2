using Idasletten.Data;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Features.Tournaments.Queries.GetTournaments;
using Idasletten.Shared.Entities;
using Idasletten.Tests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Tournaments;

public class CreateTournamentTests(IdaslettenWebApplicationFactory factory) : IClassFixture<IdaslettenWebApplicationFactory>
{
    [Fact]
    public async Task Should_PersistTournament_When_CommandIsValid()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var name = Any.String();

        // Act
        var id = await sender.Send(new CreateTournamentCommand(name, 2, 5, ScoreSystem.Elo, null, true));

        // Assert
        var tournament = await db.Tournaments.FirstAsync(t => t.Id == id);
        Assert.Equal(name, tournament.Name);
        Assert.Equal(2, tournament.TeamSize);
    }

    [Fact]
    public async Task Should_ExcludeChildTournaments_When_ListingAllTournaments()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var parentId = await sender.Send(new CreateTournamentCommand(Any.String(), 2, 5, ScoreSystem.WinCount, null, true));
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var child = Any.Tournament(parentTournamentId: parentId);
        db.Tournaments.Add(child);
        await db.SaveChangesAsync();

        // Act
        var results = await sender.Send(new GetTournamentsQuery(TournamentListScope.All));

        // Assert
        Assert.Contains(results, t => t.Id == parentId);
        Assert.DoesNotContain(results, t => t.Id == child.Id);
    }

    [Fact]
    public async Task Should_OnlyReturnPublicNonArchivedTournaments_When_ScopeIsPublic()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var publicTournament = Any.Tournament(isPublic: true, isArchived: false);
        var privateTournament = Any.Tournament(isPublic: false, isArchived: false);
        var archivedTournament = Any.Tournament(isPublic: true, isArchived: true);
        db.Tournaments.AddRange(publicTournament, privateTournament, archivedTournament);
        await db.SaveChangesAsync();

        // Act
        var results = await sender.Send(new GetTournamentsQuery(TournamentListScope.Public));

        // Assert
        Assert.Contains(results, t => t.Id == publicTournament.Id);
        Assert.DoesNotContain(results, t => t.Id == privateTournament.Id);
        Assert.DoesNotContain(results, t => t.Id == archivedTournament.Id);
    }
}
