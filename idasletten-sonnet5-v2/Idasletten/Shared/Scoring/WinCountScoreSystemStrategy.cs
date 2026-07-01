namespace Idasletten.Shared.Scoring;

/// <summary>Score = WinCount. Tie-breaking (goal difference) is applied at query/sort time.</summary>
public class WinCountScoreSystemStrategy : IScoreSystemStrategy
{
    public double InitialScore => 0;

    public void ApplyMatch(IReadOnlyList<TeamMatchInfo> teams)
    {
        foreach (var team in teams)
        {
            foreach (var player in team.Players)
            {
                player.Score = player.WinCount;
            }
        }
    }
}
