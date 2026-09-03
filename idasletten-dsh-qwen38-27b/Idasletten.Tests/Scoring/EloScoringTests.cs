using Idasletten.Models;
using Idasletten.Scoring;

namespace Idasletten.Tests.Scoring;

public class EloScoringTests
{
    private readonly EloScoring _engine = new();

    /// <summary>
    /// Applies every team of one match the way the ScoringEngine facade does:
    /// each team's delta is computed against the pre-match snapshot, so the
    /// result is independent of team order.
    /// </summary>
    private void ApplyMatch(params TeamResult[] teams)
    {
        var players = teams.SelectMany(t => t.Players).ToList();
        var pre = players.ToDictionary(p => p.Id, p => p.Score);
        var finals = new Dictionary<Guid, double>();
        foreach (var team in teams)
        {
            foreach (var p in players) p.Score = pre[p.Id];
            _engine.Apply(team.Players.ToArray(), team.Goals, teams.ToList());
            foreach (var p in team.Players) finals[p.Id] = p.Score;
        }
        foreach (var p in players)
            if (finals.TryGetValue(p.Id, out var f)) p.Score = f;
    }

    [Fact]
    public void Should_Gain16Points_When_EqualRatedPlayersWin()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        var all = TwoTeams(a, 5, b, 2);

        // Act
        ApplyMatch(all[0], all[1]);

        // Assert — 50% expected, K=32 → +16 / −16
        Approx.Equal(1516, a.Score);
        Approx.Equal(1484, b.Score);
    }

    [Fact]
    public void Should_MirrorDeltas_When_OnePlayerWins()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        var all = TwoTeams(a, 5, b, 0);

        // Act
        ApplyMatch(all[0], all[1]);

        // Assert
        Assert.True(a.Score > 1500);
        Assert.True(b.Score < 1500);
        Approx.Equal(a.Score - 1500, 1500 - b.Score);
    }

    [Fact]
    public void Should_KeepRatingUnchanged_When_GoalsAreEqual()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        var all = TwoTeams(a, 5, b, 5);

        // Act
        ApplyMatch(all[0], all[1]);

        // Assert — expected 0.5, S=0.5 → Δ=0
        Approx.Equal(1500, a.Score);
        Approx.Equal(1500, b.Score);
    }

    [Fact]
    public void Should_UseTeamAverage_When_TeamHasTwoPlayers()
    {
        // Arrange — strong duo (1600) beats weak duo (1400)
        var a1 = Any.Player(); var a2 = Any.Player();
        var b1 = Any.Player(); var b2 = Any.Player();
        _engine.Initialize(a1); _engine.Initialize(a2);
        _engine.Initialize(b1); _engine.Initialize(b2);
        a1.Score = a2.Score = 1600;
        b1.Score = b2.Score = 1400;
        var teamA = new TeamResult { Players = [a1, a2], Goals = 5 };
        var teamB = new TeamResult { Players = [b1, b2], Goals = 4 };
        var all = new List<TeamResult> { teamA, teamB };

        // Act
        ApplyMatch(teamA, teamB);

        // Assert — E ≈ 0.76 → Δ ≈ +7.6
        Approx.Equal(1607.6, a1.Score, 0.1);
        Approx.Equal(1392.4, b1.Score, 0.1);
        Approx.Equal(a1.Score, a2.Score);
    }

    [Fact]
    public void Should_ResetToBaseRating_When_PlayerIsInitialized()
    {
        // Arrange
        var p = Any.Player();
        p.Score = 1700;

        // Act
        _engine.Initialize(p);

        // Assert
        Assert.Equal(EloScoring.BaseRating, p.Score);
    }

    [Fact]
    public void Should_StoreLastDeltaAsScoreDiff_When_MatchIsApplied()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        var all = TwoTeams(a, 5, b, 0);

        // Act
        _engine.Apply([a], 5, all);

        // Assert
        Approx.Equal(16, a.ScoreDiff);
    }

    private static List<TeamResult> TwoTeams(TournamentPlayer a, int goalsA, TournamentPlayer b, int goalsB)
    {
        var teamA = new TeamResult { Players = [a], Goals = goalsA };
        var teamB = new TeamResult { Players = [b], Goals = goalsB };
        return [teamA, teamB];
    }
}
