using Idasletten.Shared.Domain;

namespace Idasletten.Shared.Scoring;

/// <summary>One team's participation in a single match during a replay.</summary>
public record TeamResult(TournamentTeam Team, List<TournamentPlayer> Players, int GoalsWon, int GoalsLost)
{
    public bool IsWinner { get; set; }
    public bool IsTie { get; set; }
}

/// <summary>
/// Calculates tournament standings for a given ScoreSystem.
/// Implementations replay matches in order to (re)compute every player's Score and aggregates,
/// which keeps editing a completed match correct (we simply recompute the whole tournament).
/// </summary>
public interface IScoreCalculator
{
    ScoreSystem System { get; }

    /// <summary>Score a freshly reset player should start with (e.g. Elo baseline).</summary>
    double InitialScore { get; }

    /// <summary>
    /// Apply a single match's outcome, mutating the players' Score/ScoreDiff.
    /// <paramref name="state"/> is a per-replay scratch pad keyed however the calculator likes
    /// (e.g. TrueSkill carries each player's Rating between matches here).
    /// </summary>
    void ApplyMatch(Tournament tournament, IReadOnlyList<TeamResult> teams, Dictionary<string, object> state);
}
