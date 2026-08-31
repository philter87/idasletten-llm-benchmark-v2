using Idasletten.Models;

namespace Idasletten.Scoring;

/// <summary>Score = number of wins. Ties are broken by goal difference, then fewer goals lost.</summary>
public sealed class WinCountScoring : IScoringEngine
{
    public ScoreSystem System => ScoreSystem.WinCount;
    public double InitialScore => 0;

    public void Initialize(TournamentPlayer player)
    {
        player.Score = 0;
        player.ScoreDiff = 0;
    }

    public void Apply(TournamentPlayer[] players, int goals, IReadOnlyList<TeamResult> allTeams)
    {
        var self = allTeams.First(t => t.Players.Any(p => players.Any(q => ReferenceEquals(q, p))));
        double diff = MatchOutcomes.Won(self, allTeams) ? 1 : 0;
        foreach (var p in players)
        {
            p.Score = p.WinCount;
            p.ScoreDiff = diff;
        }
    }
}
