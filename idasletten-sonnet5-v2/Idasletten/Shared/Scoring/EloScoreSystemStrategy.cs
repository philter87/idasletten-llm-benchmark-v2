namespace Idasletten.Shared.Scoring;

/// <summary>
/// Standard Elo, generalized to N teams by averaging pairwise expected/actual outcomes.
/// When a team has multiple players, the team's rating is the average of its players' Score.
/// </summary>
public class EloScoreSystemStrategy : IScoreSystemStrategy
{
    private const double KFactor = 32;

    public double InitialScore => 1000;

    public void ApplyMatch(IReadOnlyList<TeamMatchInfo> teams)
    {
        if (teams.Count < 2)
        {
            return;
        }

        var teamRatings = teams.Select(t => t.Players.Average(p => p.Score)).ToArray();
        var deltas = new double[teams.Count];

        for (var i = 0; i < teams.Count; i++)
        {
            for (var j = 0; j < teams.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var expected = 1.0 / (1.0 + Math.Pow(10, (teamRatings[j] - teamRatings[i]) / 400.0));
                var actual = teams[i].GoalsWon > teams[j].GoalsWon ? 1.0
                    : teams[i].GoalsWon < teams[j].GoalsWon ? 0.0
                    : 0.5;
                deltas[i] += KFactor * (actual - expected);
            }

            deltas[i] /= teams.Count - 1;
        }

        for (var i = 0; i < teams.Count; i++)
        {
            foreach (var player in teams[i].Players)
            {
                player.Score += deltas[i];
            }
        }
    }
}
