using Idasletten.Models;
using Idasletten.Scoring;

namespace Idasletten.Tests.Scoring;

public class TrueSkillScoringTests
{
    private readonly TrueSkillScoring _engine = new();

    private void ApplyMatch(params TeamResult[] teams)
    {
        var players = teams.SelectMany(t => t.Players).ToList();
        var pre = players.ToDictionary(p => p.Id, (p) => (p.Score, p.TrueSkillSigma));
        var finals = new Dictionary<Guid, (double, double)>();
        foreach (var team in teams)
        {
            foreach (var p in players) { p.Score = pre[p.Id].Item1; p.TrueSkillSigma = pre[p.Id].Item2; }
            _engine.Apply(team.Players.ToArray(), team.Goals, teams.ToList());
            foreach (var p in team.Players) finals[p.Id] = (p.Score, p.TrueSkillSigma);
        }
        foreach (var p in players)
            if (finals.TryGetValue(p.Id, out var f)) { p.Score = f.Item1; p.TrueSkillSigma = f.Item2; }
    }

    [Fact]
    public void Should_RaiseWinnerMuAndLowerLoserMu_When_MatchIsPlayed()
    {
        // Arrange
        var a = Any.Player(); var b = Any.Player();
        _engine.Initialize(a); _engine.Initialize(b);
        var all = TwoTeams(a, 5, b, 2);

        // Act
        ApplyMatch(all[0], all[1]);

        // Assert
        Assert.True(a.Score > TrueSkillScoring.InitialMean);
        Assert.True(b.Score < TrueSkillScoring.InitialMean);
        Assert.True(a.TrueSkillSigma < TrueSkillScoring.InitialSigma); // uncertainty shrinks
    }

    [Fact]
    public void Should_RaiseEveryTeamMemberMu_When_TeamWins()
    {
        // Arrange
        var a1 = Any.Player(); var a2 = Any.Player();
        var b1 = Any.Player(); var b2 = Any.Player();
        _engine.Initialize(a1); _engine.Initialize(a2);
        _engine.Initialize(b1); _engine.Initialize(b2);
        var teamA = new TeamResult { Players = [a1, a2], Goals = 5 };
        var teamB = new TeamResult { Players = [b1, b2], Goals = 1 };
        var all = new List<TeamResult> { teamA, teamB };

        // Act
        ApplyMatch(teamA, teamB);

        // Assert
        Assert.True(a1.Score > TrueSkillScoring.InitialMean);
        Assert.True(a2.Score > TrueSkillScoring.InitialMean);
        Assert.True(b1.Score < TrueSkillScoring.InitialMean);
        Assert.True(b2.Score < TrueSkillScoring.InitialMean);
    }

    [Fact]
    public void Should_InitializeMuAndSigma_When_PlayerIsCreated()
    {
        // Arrange
        var p = Any.Player();

        // Act
        _engine.Initialize(p);

        // Assert
        Assert.Equal(TrueSkillScoring.InitialMean, p.Score);
        Approx.Equal(25.0 / 3.0, p.TrueSkillSigma);
    }

    private static List<TeamResult> TwoTeams(TournamentPlayer a, int goalsA, TournamentPlayer b, int goalsB)
    {
        var teamA = new TeamResult { Players = [a], Goals = goalsA };
        var teamB = new TeamResult { Players = [b], Goals = goalsB };
        return [teamA, teamB];
    }
}
