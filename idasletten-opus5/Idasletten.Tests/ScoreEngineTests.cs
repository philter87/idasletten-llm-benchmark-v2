using Idasletten.Features.Players;
using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments;

namespace Idasletten.Tests;

public class ScoreEngineTests
{
    [Fact]
    public void Should_MoveRatingBySixteen_When_EqualEloTeamsPlay()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        ScoreEngine.Apply(tournament, Match(winner, 10, loser, 7));

        // Assert - equal ratings means an expected result of 0.5 and a K-factor of 32.
        Assert.Equal(ScoreDefaults.EloStartRating + 16, winner.Score, 3);
        Assert.Equal(ScoreDefaults.EloStartRating - 16, loser.Score, 3);
    }

    [Fact]
    public void Should_GiveLessRating_When_TheFavouriteWins()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var favourite = ResetPlayer(tournament);
        var underdog = ResetPlayer(tournament);
        favourite.Score = 1400;
        underdog.Score = 1200;

        // Act
        ScoreEngine.Apply(tournament, Match(favourite, 10, underdog, 3));

        // Assert
        Assert.InRange(favourite.Score - 1400, 0.1, 12);
    }

    [Fact]
    public void Should_AverageTeamRating_When_TeamsHaveSeveralPlayers()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo, teamSize: 2);
        var strong = ResetPlayer(tournament);
        var weak = ResetPlayer(tournament);
        var one = ResetPlayer(tournament);
        var two = ResetPlayer(tournament);
        strong.Score = 1600;
        weak.Score = 800;

        // Act - the mixed team averages to 1200, exactly as the opponents.
        ScoreEngine.Apply(tournament, new PlayedMatch(
        [
            new TeamOutcome([strong, weak], 10, 6),
            new TeamOutcome([one, two], 6, 10),
        ]));

        // Assert
        Assert.Equal(1616, strong.Score, 3);
        Assert.Equal(816, weak.Score, 3);
        Assert.Equal(1184, one.Score, 3);
    }

    [Fact]
    public void Should_RaiseConservativeRating_When_TrueSkillPlayerWins()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.TrueSkill);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        ScoreEngine.Apply(tournament, Match(winner, 5, loser, 2));

        // Assert
        Assert.True(winner.SkillMean > 25);
        Assert.True(loser.SkillMean < 25);
        Assert.True(winner.SkillDeviation < ScoreDefaults.TrueSkillInitialDeviation);
        Assert.True(winner.Score > loser.Score);
    }

    [Fact]
    public void Should_LoseOneLife_When_PlayingForLivesAndLosing()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Lives);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        ScoreEngine.Apply(tournament, Match(winner, 5, loser, 1));

        // Assert
        Assert.Equal(ScoreDefaults.StartingLives, winner.Lives);
        Assert.Equal(ScoreDefaults.StartingLives - 1, loser.Lives);
        Assert.Equal(loser.Lives, loser.Score);
    }

    [Fact]
    public void Should_NeverGoBelowZeroLives_When_LosingManyMatches()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Lives);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        for (var round = 0; round < 5; round++)
        {
            ScoreEngine.Apply(tournament, Match(winner, 5, loser, 0));
        }

        // Assert
        Assert.Equal(0, loser.Lives);
        Assert.True(loser.IsKnockedOut(ScoreSystem.Lives));
    }

    [Fact]
    public void Should_CountWins_When_ScoreSystemIsWinCount()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.WinCount);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        ScoreEngine.Apply(tournament, Match(winner, 10, loser, 8));
        ScoreEngine.Apply(tournament, Match(winner, 10, loser, 2));

        // Assert
        Assert.Equal(2, winner.Score);
        Assert.Equal(0, loser.Score);
        Assert.Equal(2, winner.WinCount);
        Assert.Equal(2, loser.LoseCount);
    }

    [Fact]
    public void Should_ShowTheChangeFromTheLastMatch_When_ScoreDiffIsRead()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        ScoreEngine.Apply(tournament, Match(winner, 10, loser, 1));

        // Assert
        Assert.Equal(16, winner.ScoreDiff, 3);
        Assert.Equal(-16, loser.ScoreDiff, 3);
    }

    [Fact]
    public void Should_CountGoalsBothWays_When_MatchIsApplied()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var (winner, loser) = TwoResetPlayers(tournament);

        // Act
        ScoreEngine.Apply(tournament, Match(winner, 10, loser, 7));

        // Assert
        Assert.Equal(10, winner.PointsWon);
        Assert.Equal(7, winner.PointsLost);
        Assert.Equal(7, loser.PointsWon);
        Assert.Equal(10, loser.PointsLost);
        Assert.Equal(1, winner.MatchCount);
    }

    [Fact]
    public void Should_GiveTheSameResult_When_TheTournamentIsRecalculated()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var (winner, loser) = TwoResetPlayers(tournament);
        var matches = new List<PlayedMatch>
        {
            Match(winner, 10, loser, 7),
            Match(loser, 10, winner, 3),
            Match(winner, 10, loser, 9),
        };

        // Act
        ScoreEngine.Recalculate(tournament, [winner, loser], matches);
        var winnerScoreAfterFirstRun = winner.Score;
        var loserScoreAfterFirstRun = loser.Score;
        ScoreEngine.Recalculate(tournament, [winner, loser], matches);

        // Assert - recalculation is a pure function of the played matches.
        Assert.Equal(winnerScoreAfterFirstRun, winner.Score, 6);
        Assert.Equal(loserScoreAfterFirstRun, loser.Score, 6);
        Assert.Equal(3, winner.MatchCount);
    }

    [Fact]
    public void Should_ForgetTheOldResult_When_AMatchIsRemovedAndScoresRecalculated()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.WinCount);
        var (winner, loser) = TwoResetPlayers(tournament);
        ScoreEngine.Recalculate(tournament, [winner, loser],
            [Match(winner, 10, loser, 1), Match(winner, 10, loser, 2)]);

        // Act - the second match is cancelled, so only one match is replayed.
        ScoreEngine.Recalculate(tournament, [winner, loser], [Match(winner, 10, loser, 1)]);

        // Assert
        Assert.Equal(1, winner.Score);
        Assert.Equal(1, winner.MatchCount);
    }

    [Fact]
    public void Should_PutTheBestPlayerFirst_When_PlayersAreRanked()
    {
        // Arrange
        var tournament = Any.Tournament(scoreSystem: ScoreSystem.Elo);
        var best = ResetPlayer(tournament);
        var middle = ResetPlayer(tournament);
        var worst = ResetPlayer(tournament);
        best.Score = 1300;
        middle.Score = 1200;
        worst.Score = 1100;

        // Act
        var ranked = ScoreEngine.Rank([worst, best, middle]).ToList();

        // Assert
        Assert.Equal([best.Id, middle.Id, worst.Id], ranked.Select(player => player.Id));
    }

    private static (TournamentPlayer Winner, TournamentPlayer Loser) TwoResetPlayers(Tournament tournament) =>
        (ResetPlayer(tournament), ResetPlayer(tournament));

    private static TournamentPlayer ResetPlayer(Tournament tournament)
    {
        var player = Any.Player(tournament.Id);
        ScoreEngine.Reset(tournament, player);
        return player;
    }

    private static PlayedMatch Match(
        TournamentPlayer home, int homeGoals, TournamentPlayer away, int awayGoals) =>
        new([new TeamOutcome([home], homeGoals, awayGoals), new TeamOutcome([away], awayGoals, homeGoals)]);
}
