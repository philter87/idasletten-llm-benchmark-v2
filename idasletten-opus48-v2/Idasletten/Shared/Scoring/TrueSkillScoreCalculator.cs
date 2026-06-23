using Idasletten.Shared.Domain;
using Moserware.Skills;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// TrueSkill via the Moserware.Skills library. Each player's <see cref="Rating"/> (mean &amp;
/// standard deviation) is carried between matches in the replay state bag; the displayed Score
/// is the conservative rating (mean − 3·stddev).
/// </summary>
public class TrueSkillScoreCalculator : IScoreCalculator
{
    private static readonly GameInfo Game = GameInfo.DefaultGameInfo;

    public ScoreSystem System => ScoreSystem.TrueSkill;

    // Conservative rating of the default rating (25 − 3·8.33 ≈ 0).
    public double InitialScore => Game.DefaultRating.ConservativeRating;

    public void ApplyMatch(Tournament tournament, IReadOnlyList<TeamResult> teams, Dictionary<string, object> state)
    {
        if (!state.TryGetValue("trueskill", out var bag))
            state["trueskill"] = bag = new Dictionary<Guid, Rating>();
        var ratings = (Dictionary<Guid, Rating>)bag;

        var players = new Dictionary<Guid, Player>();
        var skillTeams = new List<Team>();

        foreach (var team in teams)
        {
            var skillTeam = new Team();
            foreach (var p in team.Players)
            {
                var player = new Player(p.UserId);
                players[p.UserId] = player;
                skillTeam.AddPlayer(player, ratings.TryGetValue(p.UserId, out var r) ? r : Game.DefaultRating);
            }
            skillTeams.Add(skillTeam);
        }

        // Ranks: lower is better. Winner = 1, losers/ties handled by equal ranks.
        int[] ranks = teams.Select(t => t.IsWinner || t.IsTie ? 1 : 2).ToArray();

        var newRatings = TrueSkillCalculator.CalculateNewRatings(
            Game, Teams.Concat(skillTeams.ToArray()), ranks);

        foreach (var team in teams)
        {
            foreach (var p in team.Players)
            {
                var rating = newRatings[players[p.UserId]];
                ratings[p.UserId] = rating;
                double before = p.Score;
                p.Score = rating.ConservativeRating;
                p.ScoreDiff = p.Score - before;
            }
        }
    }
}
