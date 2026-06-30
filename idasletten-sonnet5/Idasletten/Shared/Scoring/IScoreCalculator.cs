using Idasletten.Features.TournamentPlayers;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// A fresh instance is created per <see cref="ScoreRecalculator"/> pass: implementations
/// may hold internal per-player working state (e.g. TrueSkill mu/sigma) that only needs to
/// live for the duration of one full chronological replay of a tournament's matches.
/// </summary>
public interface IScoreCalculator
{
    /// Resets a player's Score (and any system-specific fields) to this system's defaults.
    void ResetPlayer(TournamentPlayer player);

    /// Applies one Done match's outcome, in chronological order, mutating each player's Score
    /// (and Lives, for the Lives system). Win/loss/match counts are handled by the caller.
    void ApplyMatch(IReadOnlyList<TeamOutcome> teams);
}
