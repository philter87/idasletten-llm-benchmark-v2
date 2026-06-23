using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// TrueSkill via Moserware.Skills. Stores conservative skill estimate (mean - 3σ) in Score.
/// </summary>
public class TrueSkillScoring : IScoringSystem
{
    public const double InitialMean = 25.0;
    public const double InitialStd = 25.0 / 3.0;
    private const double Beta = 25.0 / 6.0;
    private const double Tau = 25.0 / 300.0;
    private const double DrawProbability = 0.10;
    private static readonly GameInfo GameInfo = new GameInfo(InitialMean, InitialStd, Beta, Tau, DrawProbability);

    public void Initialise(TournamentPlayer player) => player.Score = InitialMean;

    public void Apply(Tournament tournament, TournamentMatch match, ICollection<TournamentTeamMatchResult> results)
    {
        var teams = match.Teams.ToList();
        if (teams.Count != 2) return;

        var calc = new TwoTeamTrueSkillCalculator();
        var resultByTeam = results.ToDictionary(r => r.TeamId);
        if (!resultByTeam.TryGetValue(teams[0].Id, out var ra) || !resultByTeam.TryGetValue(teams[1].Id, out var rb)) return;

        // Build Moserware teams keyed by TournamentPlayer.Id (string) so we can map results back.
        var mwTeams = new List<Team>();
        var playerMap = new Dictionary<string, TournamentPlayer>();
        foreach (var team in teams)
        {
            var mwTeam = new Team();
            foreach (var p in team.Players)
            {
                var key = p.Id.ToString("N");
                playerMap[key] = p;
                var rating = new Rating(p.Score == InitialMean ? InitialMean : p.Score, InitialStd);
                mwTeam.AddPlayer(new Player(key), rating);
            }
            mwTeams.Add(mwTeam);
        }

        // Match.mathc teams order corresponds to rank: lower rank = winner.
        var rankA = ra.GoalsWon > ra.GoalsLost ? 1 : (ra.GoalsWon == ra.GoalsLost ? 1 : 2);
        var rankB = rankA == 1 ? 2 : 1;

        var concat = Teams.Concat(mwTeams[0], mwTeams[1]);
        var newRatings = calc.CalculateNewRatings(GameInfo, concat, rankA, rankB);

        foreach (var kv in newRatings)
        {
            var key = ((Player)kv.Key).Id.ToString();
            if (playerMap.TryGetValue(key, out var p))
            {
                var oldScore = p.Score;
                var newScore = kv.Value.Mean - 3 * kv.Value.StandardDeviation;
                p.Score = newScore;
                p.ScoreDiff = (int)Math.Round(newScore - oldScore);
            }
        }
    }
}