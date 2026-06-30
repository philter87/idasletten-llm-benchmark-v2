using Idasletten.Features.TournamentPlayers;
using Moserware.Skills;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Uses Moserware.Skills.Core's TrueSkillCalculator. Mu/sigma aren't stored on
/// TournamentPlayer (it only has a single Score double), so this calculator keeps its own
/// working ratings dictionary for the lifetime of one ScoreRecalculator pass and writes the
/// conservative rating (mu - 3*sigma) to Score after each match.
/// </summary>
public class TrueSkillScoreCalculator : IScoreCalculator
{
    private static readonly GameInfo GameInfo = GameInfo.DefaultGameInfo;
    private readonly Dictionary<Guid, Rating> _ratings = new();

    public void ResetPlayer(TournamentPlayer player)
    {
        _ratings[player.Id] = GameInfo.DefaultRating;
        player.Score = GameInfo.DefaultRating.ConservativeRating;
    }

    public void ApplyMatch(IReadOnlyList<TeamOutcome> teamOutcomes)
    {
        if (teamOutcomes.Count < 2) return;

        var teams = teamOutcomes
            .Select(outcome =>
            {
                var team = new Team<Guid>();
                foreach (var player in outcome.Players)
                {
                    team.AddPlayer(player.Id, RatingFor(player.Id));
                }
                return team;
            })
            .ToArray();

        var ranks = RanksByNetGoalsDescending(teamOutcomes);
        var newRatings = TrueSkillCalculator.CalculateNewRatings(GameInfo, Teams.Concat(teams), ranks);

        foreach (var (playerId, rating) in newRatings)
        {
            _ratings[playerId] = rating;
        }

        foreach (var player in teamOutcomes.SelectMany(t => t.Players))
        {
            player.Score = RatingFor(player.Id).ConservativeRating;
        }
    }

    private Rating RatingFor(Guid playerId) =>
        _ratings.TryGetValue(playerId, out var rating) ? rating : GameInfo.DefaultRating;

    /// Rank 1 = best net goals; tied teams share a rank.
    private static int[] RanksByNetGoalsDescending(IReadOnlyList<TeamOutcome> teamOutcomes)
    {
        var ordered = teamOutcomes
            .Select((outcome, index) => (outcome, index))
            .OrderByDescending(x => x.outcome.NetGoals)
            .ToList();

        var ranks = new int[teamOutcomes.Count];
        var rank = 1;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (i > 0 && ordered[i].outcome.NetGoals < ordered[i - 1].outcome.NetGoals)
            {
                rank = i + 1;
            }
            ranks[ordered[i].index] = rank;
        }
        return ranks;
    }
}
