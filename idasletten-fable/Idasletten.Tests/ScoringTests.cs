using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class ScoringTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ScoringTests(TestWebApplicationFactory factory) => _factory = factory;

    private async Task<(IServiceScope Scope, IMediator Mediator, AppDbContext Db, Tournament Tournament, string[] Initials)>
        SetupTournament(ScoreSystem scoreSystem, int teamSize = 1, int playerCount = 2)
    {
        var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tournament = await mediator.Send(new CreateTournamentCommand(
            Any.TournamentName(), TeamSize: teamSize, ScoreSystem: scoreSystem));
        var initials = Enumerable.Range(0, playerCount).Select(_ => Any.Initials()).ToArray();
        foreach (var i in initials)
            await mediator.Send(new AddPlayerToTournamentCommand(tournament.Id, i));
        return (scope, mediator, db, tournament, initials);
    }

    private static async Task<TournamentPlayer> PlayerOf(AppDbContext db, Guid tournamentId, string initials) =>
        await db.TournamentPlayers.AsNoTracking()
            .Include(p => p.User)
            .SingleAsync(p => p.TournamentId == tournamentId && p.User.UserName == initials);

    [Fact]
    public async Task Should_TransferEloPointsFromLoserToWinner_When_MatchIsRecorded()
    {
        // Arrange
        var (scope, mediator, db, tournament, initials) = await SetupTournament(ScoreSystem.Elo);
        using var _ = scope;

        // Act
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([initials[0]], 5), new TeamResultInput([initials[1]], 3)]));

        // Assert — equal ratings, K=32: winner gains exactly 16.
        var winner = await PlayerOf(db, tournament.Id, initials[0]);
        var loser = await PlayerOf(db, tournament.Id, initials[1]);
        Assert.Equal(ScoringEngine.EloInitialScore + 16, winner.Score);
        Assert.Equal(ScoringEngine.EloInitialScore - 16, loser.Score);
        Assert.Equal(16, winner.ScoreDiff);
        Assert.Equal(-16, loser.ScoreDiff);
        Assert.Equal(1, winner.WinCount);
        Assert.Equal(1, loser.LoseCount);
        Assert.Equal(5, winner.PointsWon);
        Assert.Equal(3, winner.PointsLost);
    }

    [Fact]
    public async Task Should_IncreaseTrueSkillOfWinner_When_MatchIsRecorded()
    {
        // Arrange
        var (scope, mediator, db, tournament, initials) = await SetupTournament(ScoreSystem.TrueSkill);
        using var _ = scope;

        // Act
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([initials[0]], 5), new TeamResultInput([initials[1]], 1)]));

        // Assert
        var winner = await PlayerOf(db, tournament.Id, initials[0]);
        var loser = await PlayerOf(db, tournament.Id, initials[1]);
        Assert.True(winner.TrueSkillMean > loser.TrueSkillMean);
        Assert.True(winner.Score > loser.Score);
        Assert.True(winner.TrueSkillStdDev < 25.0 / 3);
    }

    [Fact]
    public async Task Should_RemoveOneLifeFromLosers_When_LivesTournamentMatchIsRecorded()
    {
        // Arrange
        var (scope, mediator, db, tournament, initials) = await SetupTournament(ScoreSystem.Lives);
        using var _ = scope;

        // Act
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([initials[0]], 5), new TeamResultInput([initials[1]], 2)]));

        // Assert
        var winner = await PlayerOf(db, tournament.Id, initials[0]);
        var loser = await PlayerOf(db, tournament.Id, initials[1]);
        Assert.Equal(3, winner.Lives);
        Assert.Equal(2, loser.Lives);
        Assert.Equal(2, loser.Score);
    }

    [Fact]
    public async Task Should_SetScoreToWinCount_When_WinCountTournamentMatchIsRecorded()
    {
        // Arrange
        var (scope, mediator, db, tournament, initials) = await SetupTournament(ScoreSystem.WinCount);
        using var _ = scope;

        // Act
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([initials[0]], 5), new TeamResultInput([initials[1]], 0)]));
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
            [new TeamResultInput([initials[0]], 5), new TeamResultInput([initials[1]], 4)]));

        // Assert
        var winner = await PlayerOf(db, tournament.Id, initials[0]);
        Assert.Equal(2, winner.Score);
        Assert.Equal(2, winner.WinCount);
    }

    [Fact]
    public async Task Should_AverageTeamRatings_When_EloTeamsHaveMultiplePlayers()
    {
        // Arrange
        var (scope, mediator, db, tournament, initials) =
            await SetupTournament(ScoreSystem.Elo, teamSize: 2, playerCount: 4);
        using var _ = scope;

        // Act
        await mediator.Send(new RecordMatchResultCommand(tournament.Id,
        [
            new TeamResultInput([initials[0], initials[1]], 5),
            new TeamResultInput([initials[2], initials[3]], 4)
        ]));

        // Assert — every player on the winning team gets the same +16 delta.
        foreach (var winnerInitials in initials.Take(2))
        {
            var player = await PlayerOf(db, tournament.Id, winnerInitials);
            Assert.Equal(ScoringEngine.EloInitialScore + 16, player.Score);
        }
    }
}
