using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

/// <summary>
/// WinCount scoring: Score equals WinCount. Tie-breaker is goal difference.
/// </summary>
public class WinCountScoringService : IScoringService
{
    public void CalculateScores(
        List<TournamentPlayer> team1Players,
        List<TournamentPlayer> team2Players,
        int team1Goals,
        int team2Goals,
        Tournament tournament)
    {
        bool team1Won = team1Goals > team2Goals;
        bool team2Won = team2Goals > team1Goals;

        foreach (var player in team1Players)
        {
            player.MatchCount++;
            player.PointsWon += team1Goals;
            player.PointsLost += team2Goals;
            if (team1Won)
            {
                player.WinCount++;
                player.ScoreDiff = 1;
            }
            else if (team2Won)
            {
                player.LoseCount++;
                player.ScoreDiff = 0;
            }
            player.Score = player.WinCount;
        }

        foreach (var player in team2Players)
        {
            player.MatchCount++;
            player.PointsWon += team2Goals;
            player.PointsLost += team1Goals;
            if (team2Won)
            {
                player.WinCount++;
                player.ScoreDiff = 1;
            }
            else if (team1Won)
            {
                player.LoseCount++;
                player.ScoreDiff = 0;
            }
            player.Score = player.WinCount;
        }
    }
}
