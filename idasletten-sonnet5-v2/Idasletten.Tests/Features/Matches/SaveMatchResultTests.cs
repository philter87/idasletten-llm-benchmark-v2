using Idasletten.Data;
using Idasletten.Features.Matches.Commands.SaveMatchResult;
using Idasletten.Shared.Entities;
using Idasletten.Tests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Matches;

public class SaveMatchResultTests(IdaslettenWebApplicationFactory factory) : IClassFixture<IdaslettenWebApplicationFactory>
{
    [Fact]
    public async Task Should_UpdateEloScoresAndMarkMatchDone_When_ResultIsSaved()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var winnerInitials = Any.Initials();
        var loserInitials = Any.Initials();

        // Act
        await sender.Send(new SaveMatchResultCommand(
            Guid.NewGuid(),
            tournament.Id,
            [new MatchTeamInput([winnerInitials], 5), new MatchTeamInput([loserInitials], 2)],
            IsEditAuthorized: false));

        // Assert
        var players = await db.TournamentPlayers.Include(p => p.User).Where(p => p.TournamentId == tournament.Id).ToListAsync();
        var winner = players.Single(p => p.User.UserName == winnerInitials);
        var loser = players.Single(p => p.User.UserName == loserInitials);
        Assert.Equal(1016, winner.Score);
        Assert.Equal(984, loser.Score);
        Assert.Equal(1, winner.WinCount);
        Assert.Equal(1, loser.LoseCount);
        Assert.Equal(MatchState.Done, await db.TournamentMatches.Where(m => m.TournamentId == tournament.Id).Select(m => m.State).SingleAsync());
    }

    [Fact]
    public async Task Should_Throw_When_EditingACompletedMatchWithoutAuthorization()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var tournament = Any.Tournament();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var matchId = Guid.NewGuid();
        await sender.Send(new SaveMatchResultCommand(
            matchId,
            tournament.Id,
            [new MatchTeamInput([Any.Initials()], 5), new MatchTeamInput([Any.Initials()], 1)],
            IsEditAuthorized: false));

        // Act & Assert: the match is now Done, so editing it again requires authorization.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sender.Send(new SaveMatchResultCommand(
            matchId,
            tournament.Id,
            [new MatchTeamInput([Any.Initials()], 5), new MatchTeamInput([Any.Initials()], 1)],
            IsEditAuthorized: false)));
    }

    [Fact]
    public async Task Should_RecalculateScoresFromScratch_When_AuthorizedEditChangesAnEarlierMatch()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.WinCount);
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var a = Any.Initials();
        var b = Any.Initials();
        var matchId = Guid.NewGuid();
        await sender.Send(new SaveMatchResultCommand(matchId, tournament.Id,
            [new MatchTeamInput([a], 5), new MatchTeamInput([b], 1)], IsEditAuthorized: false));

        // Act: flip the result of the same match, now authorized.
        await sender.Send(new SaveMatchResultCommand(matchId, tournament.Id,
            [new MatchTeamInput([a], 1), new MatchTeamInput([b], 5)], IsEditAuthorized: true));

        // Assert
        var players = await db.TournamentPlayers.Include(p => p.User).Where(p => p.TournamentId == tournament.Id).ToListAsync();
        var playerA = players.Single(p => p.User.UserName == a);
        var playerB = players.Single(p => p.User.UserName == b);
        Assert.Equal(0, playerA.WinCount);
        Assert.Equal(1, playerA.LoseCount);
        Assert.Equal(1, playerB.WinCount);
        Assert.Equal(0, playerB.LoseCount);
    }
}
