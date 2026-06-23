using Idasletten.Shared.Domain;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Standard Elo. A team's rating is the average of its players' ratings; the resulting
/// rating change is applied equally to every player on the team.
/// </summary>
public class EloScoreCalculator : IScoreCalculator
{
    private const double K = 32;

    public ScoreSystem System => ScoreSystem.Elo;
    public double InitialScore => 1000;

    public void ApplyMatch(Tournament tournament, IReadOnlyList<TeamResult> teams, Dictionary<string, object> state)
    {
        // Elo is defined for two sides. With more than two teams we treat each team
        // against the average of all the others.
        var averages = teams.ToDictionary(t => t, t => t.Players.Average(p => p.Score));

        foreach (var team in teams)
        {
            var others = teams.Where(t => t != team).ToList();
            double opponentAvg = others.Average(o => averages[o]);
            double expected = 1.0 / (1.0 + Math.Pow(10, (opponentAvg - averages[team]) / 400.0));
            double actual = team.IsTie ? 0.5 : team.IsWinner ? 1.0 : 0.0;
            double delta = K * (actual - expected);

            foreach (var player in team.Players)
            {
                double before = player.Score;
                player.Score = before + delta;
                player.ScoreDiff = player.Score - before;
            }
        }
    }
}
