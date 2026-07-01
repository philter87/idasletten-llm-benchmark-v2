using Moserware.Skills;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Uses the Moserware.Skills(.Core) TrueSkill implementation. Ratings are kept in a
/// dictionary local to this strategy instance, which the recalculator creates fresh
/// for each full tournament recompute pass (see TournamentScoreRecalculator).
/// </summary>
public class TrueSkillScoreSystemStrategy : IScoreSystemStrategy
{
    private static readonly GameInfo GameInfo = GameInfo.DefaultGameInfo;

    private readonly Dictionary<Guid, Rating> _ratings = new();

    public double InitialScore => GameInfo.DefaultRating.ConservativeRating;

    public void ApplyMatch(IReadOnlyList<TeamMatchInfo> teams)
    {
        if (teams.Count < 2)
        {
            return;
        }

        var teamObjects = new List<Team<Guid>>();
        foreach (var team in teams)
        {
            Team<Guid>? teamObject = null;
            foreach (var player in team.Players)
            {
                var rating = _ratings.TryGetValue(player.Id, out var existing) ? existing : GameInfo.DefaultRating;
                teamObject = teamObject is null
                    ? new Team<Guid>(player.Id, rating)
                    : teamObject.AddPlayer(player.Id, rating);
            }
            teamObjects.Add(teamObject!);
        }

        var sortedGoals = teams.Select(t => t.GoalsWon).Distinct().OrderByDescending(g => g).ToList();
        var ranks = teams.Select(t => sortedGoals.IndexOf(t.GoalsWon) + 1).ToArray();

        var newRatings = TrueSkillCalculator.CalculateNewRatings(GameInfo, Teams.Concat(teamObjects.ToArray()), ranks);

        foreach (var team in teams)
        {
            foreach (var player in team.Players)
            {
                var newRating = newRatings[player.Id];
                _ratings[player.Id] = newRating;
                player.Score = newRating.ConservativeRating;
            }
        }
    }
}
