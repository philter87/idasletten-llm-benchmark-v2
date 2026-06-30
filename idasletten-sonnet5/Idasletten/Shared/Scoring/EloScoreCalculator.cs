using Idasletten.Features.TournamentPlayers;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Standard Elo. Team rating = average of member ratings. For more than two teams in a
/// single match, every team pair plays a virtual head-to-head (ranked by net goals), and
/// each pairwise delta is scaled by 1/(teamCount-1) so total movement stays comparable to
/// a normal two-team match.
/// </summary>
public class EloScoreCalculator : IScoreCalculator
{
    private const double K = 32;
    private const double StartingRating = 1200;

    public void ResetPlayer(TournamentPlayer player) => player.Score = StartingRating;

    public void ApplyMatch(IReadOnlyList<TeamOutcome> teams)
    {
        if (teams.Count < 2) return;

        var teamRatings = teams.ToDictionary(t => t.TeamId, t => t.Players.Average(p => p.Score));
        var deltas = teams.ToDictionary(t => t.TeamId, _ => 0.0);

        for (var i = 0; i < teams.Count; i++)
        {
            for (var j = i + 1; j < teams.Count; j++)
            {
                var teamA = teams[i];
                var teamB = teams[j];
                var ratingA = teamRatings[teamA.TeamId];
                var ratingB = teamRatings[teamB.TeamId];
                var expectedA = 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
                var actualA = teamA.NetGoals > teamB.NetGoals ? 1.0
                    : teamA.NetGoals < teamB.NetGoals ? 0.0
                    : 0.5;
                var delta = K * (actualA - expectedA) / (teams.Count - 1);
                deltas[teamA.TeamId] += delta;
                deltas[teamB.TeamId] -= delta;
            }
        }

        foreach (var team in teams)
        {
            foreach (var player in team.Players)
            {
                player.Score += deltas[team.TeamId];
            }
        }
    }
}
