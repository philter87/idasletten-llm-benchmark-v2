using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class MatchTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MatchTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_CompletePlannedMatch_When_ResultIsRecordedWithItsId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tournament = await mediator.Send(new CreateTournamentCommand(Any.TournamentName(), TeamSize: 1));
        var a = Any.Initials();
        var b = Any.Initials();
        var planned = await mediator.Send(new PlanMatchCommand(tournament.Id, [[a], [b]]));

        // Act
        var done = await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([a], 5), new TeamResultInput([b], 2)], planned.Id));

        // Assert
        Assert.Equal(planned.Id, done.Id);
        var reloaded = await db.TournamentMatches.AsNoTracking().SingleAsync(m => m.Id == planned.Id);
        Assert.Equal(MatchState.Done, reloaded.State);
    }

    [Fact]
    public async Task Should_ReuseTeam_When_SamePlayersPlayTogetherAgain()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tournament = await mediator.Send(new CreateTournamentCommand(Any.TournamentName(), TeamSize: 2));
        var initials = Enumerable.Range(0, 4).Select(_ => Any.Initials()).ToArray();

        // Act
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
        [
            new TeamResultInput([initials[0], initials[1]], 5),
            new TeamResultInput([initials[2], initials[3]], 3)
        ]));
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
        [
            new TeamResultInput([initials[0], initials[1]], 5),
            new TeamResultInput([initials[2], initials[3]], 1)
        ]));

        // Assert — the same two teams are reused, not duplicated.
        var teamCount = await db.TournamentTeams.CountAsync(t => t.TournamentId == tournament.Id);
        Assert.Equal(2, teamCount);
    }

    [Fact]
    public async Task Should_RecalculateScores_When_DoneMatchIsEdited()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tournament = await mediator.Send(new CreateTournamentCommand(
            Any.TournamentName(), TeamSize: 1, ScoreSystem: ScoreSystem.Elo));
        var a = Any.Initials();
        var b = Any.Initials();
        var match = await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([a], 5), new TeamResultInput([b], 2)]));

        // Act — flip the result: b now won.
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([a], 2), new TeamResultInput([b], 5)], match.Id));

        // Assert — the tournament was replayed from scratch with the new result.
        var playerA = await db.TournamentPlayers.AsNoTracking().Include(p => p.User)
            .SingleAsync(p => p.TournamentId == tournament.Id && p.User.UserName == a);
        var playerB = await db.TournamentPlayers.AsNoTracking().Include(p => p.User)
            .SingleAsync(p => p.TournamentId == tournament.Id && p.User.UserName == b);
        Assert.Equal(ScoringEngine.EloInitialScore - 16, playerA.Score);
        Assert.Equal(ScoringEngine.EloInitialScore + 16, playerB.Score);
        Assert.Equal(0, playerA.WinCount);
        Assert.Equal(1, playerA.LoseCount);
        Assert.Equal(1, playerB.WinCount);
        Assert.Equal(1, await db.TournamentMatches.CountAsync(m => m.TournamentId == tournament.Id));
    }
}
