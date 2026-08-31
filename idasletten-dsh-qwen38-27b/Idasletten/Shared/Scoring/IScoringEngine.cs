using Idasletten.Models;

namespace Idasletten.Scoring;

/// <summary>One of the four tournament score systems.</summary>
public interface IScoringEngine
{
    ScoreSystem System { get; }

    /// <summary>Initial score for a freshly added player (1500 Elo, 25 TrueSkill mu, 3 lives, 0 wins).</summary>
    double InitialScore { get; }

    /// <summary>Reset a player's score state to the initial values for this system.</summary>
    void Initialize(TournamentPlayer player);

    /// <summary>
    /// Apply one finished match to the given teams, updating every player's
    /// score state (Score, counters, ScoreDiff). Common counters (MatchCount,
    /// PointsWon/Lost, Win/Lose) are updated by the facade, not here.
    /// </summary>
    void Apply(TournamentPlayer[] players, int goals, IReadOnlyList<TeamResult> allTeams);
}
