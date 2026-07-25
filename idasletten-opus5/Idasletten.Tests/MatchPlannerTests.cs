using Idasletten.Features.Matches;

namespace Idasletten.Tests;

public class MatchPlannerTests
{
    private static readonly Random Deterministic = new(1);

    [Fact]
    public void Should_PairBestWithWorst_When_SeedingIsEquality()
    {
        // Arrange - twelve players, best first, gives six teams and three games.
        var players = Players(12);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 1, fixedTeams: false,
            SeedingType.Equality, Deterministic);

        // Assert - 1+12, 2+11, 3+10, 4+9, 5+8 and 6+7.
        var teams = games.SelectMany(game => game.Teams).ToList();
        Assert.Equal(6, teams.Count);
        Assert.Contains(teams, team => Same(team, players[0], players[11]));
        Assert.Contains(teams, team => Same(team, players[1], players[10]));
        Assert.Contains(teams, team => Same(team, players[5], players[6]));
    }

    [Fact]
    public void Should_PairTopHalfWithBottomHalf_When_SeedingIsFair()
    {
        // Arrange - the example from the specification: ten players give 1+6, 2+7, 3+8, 4+9 and 5+10.
        var players = Players(10);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 1, fixedTeams: false,
            SeedingType.Fair, Deterministic);

        // Assert - five teams cannot all play at once, so the fifth team sits over this round.
        var teams = games.SelectMany(game => game.Teams).ToList();
        Assert.Equal(4, teams.Count);
        Assert.Contains(teams, team => Same(team, players[0], players[5]));
        Assert.Contains(teams, team => Same(team, players[1], players[6]));
        Assert.Contains(teams, team => Same(team, players[3], players[8]));
    }

    [Fact]
    public void Should_LetEverybodyPlayWithTheirHalf_When_SeedingIsFairAndTeamsFitTheRound()
    {
        // Arrange - twelve players give six teams, so all of them play.
        var players = Players(12);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 1, fixedTeams: false,
            SeedingType.Fair, Deterministic);

        // Assert - top half is 1-6, bottom half is 7-12: 1+7, 2+8 ... 6+12.
        var teams = games.SelectMany(game => game.Teams).ToList();
        Assert.Equal(6, teams.Count);
        Assert.Contains(teams, team => Same(team, players[0], players[6]));
        Assert.Contains(teams, team => Same(team, players[5], players[11]));
    }

    [Fact]
    public void Should_CreateOneGamePerPlayerPerRound_When_PlayersDivideEvenly()
    {
        // Arrange - eight players in teams of two is four teams, that is two games per round.
        var players = Players(8);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 3, fixedTeams: false,
            SeedingType.Random, Deterministic);

        // Assert
        Assert.Equal(6, games.Count);
        Assert.Equal(6, MatchPlanner.GameCount(playerCount: 8, teamSize: 2, gamesPerPlayer: 3));
        Assert.All(players, player =>
            Assert.Equal(3, games.Count(game => game.Teams.Any(team => team.PlayerIds.Contains(player)))));
    }

    [Fact]
    public void Should_KeepTheSameTeams_When_FixedTeamsIsChosen()
    {
        // Arrange
        var players = Players(8);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 3, fixedTeams: true,
            SeedingType.Equality, Deterministic);

        // Assert - only four distinct teams exist across all three rounds.
        var distinctTeams = games
            .SelectMany(game => game.Teams)
            .Select(team => string.Join(",", team.PlayerIds.OrderBy(id => id)))
            .Distinct()
            .ToList();

        Assert.Equal(4, distinctTeams.Count);
    }

    [Fact]
    public void Should_ChangeTeams_When_TeamsAreNotFixed()
    {
        // Arrange
        var players = Players(8);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 3, fixedTeams: false,
            SeedingType.Equality, Deterministic);

        // Assert
        var distinctTeams = games
            .SelectMany(game => game.Teams)
            .Select(team => string.Join(",", team.PlayerIds.OrderBy(id => id)))
            .Distinct()
            .ToList();

        Assert.True(distinctTeams.Count > 4);
    }

    [Fact]
    public void Should_PlanNothing_When_ThereAreTooFewPlayers()
    {
        // Arrange - three players cannot fill two teams of two.
        var players = Players(3);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 2, fixedTeams: false,
            SeedingType.Fair, Deterministic);

        // Assert
        Assert.Empty(games);
    }

    [Fact]
    public void Should_LetPlayersSitOverInTurn_When_TheCountDoesNotAddUp()
    {
        // Arrange - six players in teams of two gives three teams, so one team sits over every round.
        var players = Players(6);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 2, gamesPerPlayer: 3, fixedTeams: false,
            SeedingType.Equality, Deterministic);

        // Assert - everybody gets at least one game instead of the same two sitting over every time.
        Assert.Equal(3, games.Count);
        Assert.All(players, player =>
            Assert.True(games.Any(game => game.Teams.Any(team => team.PlayerIds.Contains(player)))));
    }

    [Fact]
    public void Should_PlanSinglesMatches_When_TeamSizeIsOne()
    {
        // Arrange
        var players = Players(4);

        // Act
        var games = MatchPlanner.Plan(players, teamSize: 1, gamesPerPlayer: 2, fixedTeams: false,
            SeedingType.Random, Deterministic);

        // Assert
        Assert.Equal(4, games.Count);
        Assert.All(games, game => Assert.All(game.Teams, team => Assert.Single(team.PlayerIds)));
    }

    private static List<Guid> Players(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    private static bool Same(MatchPlanner.PlannedTeam team, params Guid[] playerIds) =>
        team.PlayerIds.Count == playerIds.Length && playerIds.All(team.PlayerIds.Contains);
}
