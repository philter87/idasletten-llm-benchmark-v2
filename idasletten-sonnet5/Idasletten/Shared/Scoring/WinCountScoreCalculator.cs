using Idasletten.Features.TournamentPlayers;

namespace Idasletten.Shared.Scoring;

/// <summary>Score = WinCount. Tie-break for any ranked display is goal difference.</summary>
public class WinCountScoreCalculator : IScoreCalculator
{
    public void ResetPlayer(TournamentPlayer player) => player.Score = 0;

    public void ApplyMatch(IReadOnlyList<TeamOutcome> teams)
    {
        if (teams.Count < 2) return;

        var bestNet = teams.Max(t => t.NetGoals);
        foreach (var team in teams.Where(t => t.NetGoals == bestNet))
        {
            foreach (var player in team.Players)
            {
                player.Score += 1;
            }
        }
    }
}
