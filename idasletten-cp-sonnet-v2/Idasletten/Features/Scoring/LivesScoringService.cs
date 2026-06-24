using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

/// <summary>
/// Lives scoring: losing a match costs one life. Score is the remaining lives.
/// When a player reaches 0 lives, they are eliminated.
/// </summary>
public class LivesScoringService : IScoringService
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
                player.ScoreDiff = 0;
            }
            else if (team2Won)
            {
                player.LoseCount++;
                player.Lives = Math.Max(0, player.Lives - 1);
                player.ScoreDiff = -1;
            }
            player.Score = player.Lives;
        }

        foreach (var player in team2Players)
        {
            player.MatchCount++;
            player.PointsWon += team2Goals;
            player.PointsLost += team1Goals;
            if (team2Won)
            {
                player.WinCount++;
                player.ScoreDiff = 0;
            }
            else if (team1Won)
            {
                player.LoseCount++;
                player.Lives = Math.Max(0, player.Lives - 1);
                player.ScoreDiff = -1;
            }
            player.Score = player.Lives;
        }
    }
}
