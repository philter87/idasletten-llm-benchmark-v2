using Idasletten.Shared.Domain;

namespace Idasletten.Shared.Scoring;

/// <summary>Score == number of wins. Tie-breaking by goal difference is handled at query time.</summary>
public class WinCountScoreCalculator : IScoreCalculator
{
    public ScoreSystem System => ScoreSystem.WinCount;
    public double InitialScore => 0;

    public void ApplyMatch(Tournament tournament, IReadOnlyList<TeamResult> teams, Dictionary<string, object> state)
    {
        foreach (var team in teams)
        {
            foreach (var player in team.Players)
            {
                double before = player.Score;
                player.Score = player.WinCount; // WinCount already updated by the orchestrator
                player.ScoreDiff = player.Score - before;
            }
        }
    }
}
