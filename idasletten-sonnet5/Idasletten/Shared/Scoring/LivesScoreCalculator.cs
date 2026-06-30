using Idasletten.Features.TournamentPlayers;

namespace Idasletten.Shared.Scoring;

/// <summary>Score = remaining Lives. A loss (lowest net goals) costs one life, floored at 0.</summary>
public class LivesScoreCalculator : IScoreCalculator
{
    private const int StartingLives = 3;

    public void ResetPlayer(TournamentPlayer player)
    {
        player.Lives = StartingLives;
        player.Score = StartingLives;
    }

    public void ApplyMatch(IReadOnlyList<TeamOutcome> teams)
    {
        if (teams.Count < 2) return;

        var bestNet = teams.Max(t => t.NetGoals);
        foreach (var team in teams)
        {
            if (team.NetGoals < bestNet)
            {
                foreach (var player in team.Players)
                {
                    player.Lives = Math.Max(0, player.Lives - 1);
                }
            }
        }

        foreach (var player in teams.SelectMany(t => t.Players))
        {
            player.Score = player.Lives;
        }
    }
}
