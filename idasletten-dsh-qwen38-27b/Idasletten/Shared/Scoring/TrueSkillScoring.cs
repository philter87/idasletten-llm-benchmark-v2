using Idasletten.Models;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;

namespace Idasletten.Scoring;

/// <summary>
/// TrueSkill via the vendored Moserware.Skills library. <see cref="TournamentPlayer.Score"/>
/// stores mu and <see cref="TournamentPlayer.TrueSkillSigma"/> stores sigma.
/// Multi-team matches are ranked by goals (ties repeat the rank).
/// </summary>
public sealed class TrueSkillScoring : IScoringEngine
{
    public const double InitialMean = 25;
    public const double InitialSigma = 25.0 / 3.0;

    public ScoreSystem System => ScoreSystem.TrueSkill;
    public double InitialScore => InitialMean;

    private static readonly GameInfo GameInfo = new(
        InitialMean, InitialSigma,
        beta: InitialMean / 6.0,
        dynamicFactor: InitialMean / 300.0,
        drawProbability: 0.10);

    public void Initialize(TournamentPlayer player)
    {
        player.Score = InitialMean;
        player.TrueSkillSigma = InitialSigma;
        player.ScoreDiff = 0;
    }

    public void Apply(TournamentPlayer[] players, int goals, IReadOnlyList<TeamResult> allTeams)
    {
        var map = new Dictionary<Guid, (TournamentPlayer Player, double OldMu)>();
        foreach (var t in allTeams)
            foreach (var p in t.Players)
                map[p.Id] = (p, p.Score);

        var teams = allTeams
            .Select(t => (IDictionary<Player<Guid>, Rating>)new Dictionary<Player<Guid>, Rating>(
                t.Players.Select(p => new KeyValuePair<Player<Guid>, Rating>(
                    new Player<Guid>(p.Id), new Rating(p.Score, p.TrueSkillSigma)))))
            .ToList();
        var ranks = allTeams.Select(t => MatchOutcomes.Rank(t, allTeams)).ToArray();

        var result = TrueSkillCalculator.CalculateNewRatings(
            GameInfo,
            teams.Select(t => (IDictionary<Player<Guid>, Rating>)t).ToList(),
            ranks);

        foreach (var (playerId, (player, oldMu)) in map)
        {
            var rating = result.First(kv => kv.Key.Id == playerId).Value;
            player.Score = rating.Mean;
            player.TrueSkillSigma = rating.StandardDeviation;
            player.ScoreDiff = rating.Mean - oldMu;
        }
    }
}
