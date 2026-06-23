using Idasletten.Shared.Domain;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Players start with a fixed number of lives (default 3). Losing a match costs a life.
/// Score mirrors the remaining lives so the scoreboard can order by it.
/// </summary>
public class LivesScoreCalculator : IScoreCalculator
{
    public ScoreSystem System => ScoreSystem.Lives;
    public double InitialScore => 0;

    public void ApplyMatch(Tournament tournament, IReadOnlyList<TeamResult> teams, Dictionary<string, object> state)
    {
        foreach (var team in teams)
        {
            foreach (var player in team.Players)
            {
                double before = player.Score;
                if (!team.IsWinner && !team.IsTie && player.Lives > 0)
                {
                    player.Lives -= 1;
                }
                player.Score = player.Lives;
                player.ScoreDiff = player.Score - before;
            }
        }
    }
}
