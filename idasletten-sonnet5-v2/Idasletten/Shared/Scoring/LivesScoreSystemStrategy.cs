namespace Idasletten.Shared.Scoring;

/// <summary>Losing a game costs a life. Score mirrors the player's remaining Lives.</summary>
public class LivesScoreSystemStrategy : IScoreSystemStrategy
{
    public double InitialScore => 3;

    public void ApplyMatch(IReadOnlyList<TeamMatchInfo> teams)
    {
        var maxGoals = teams.Max(t => t.GoalsWon);
        var isSoleWinner = teams.Count(t => t.GoalsWon == maxGoals) == 1;

        foreach (var team in teams)
        {
            var isLoser = isSoleWinner && team.GoalsWon != maxGoals;
            foreach (var player in team.Players)
            {
                if (isLoser)
                {
                    player.Lives = Math.Max(0, player.Lives - 1);
                }
                player.Score = player.Lives;
            }
        }
    }
}
