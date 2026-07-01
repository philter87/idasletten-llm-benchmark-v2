namespace Idasletten.Shared.Scoring;

public interface IScoreSystemStrategy
{
    /// <summary>Starting value for TournamentPlayer.Score when a player joins / is reset.</summary>
    double InitialScore { get; }

    /// <summary>
    /// Updates Score (and, for the Lives system, Lives) on the players of each team based on
    /// this match's result. WinCount/LoseCount/MatchCount/PointsWon/PointsLost/ScoreDiff are
    /// handled generically by the caller.
    /// </summary>
    void ApplyMatch(IReadOnlyList<TeamMatchInfo> teams);
}
