using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Scoring;

public class WinCountScoringSystem : IWinCountScoringSystem
{
    public async Task CalculateMatchResultsAsync(TournamentMatch match, Tournament tournament)
    {
        // WinCount scoring is handled per-player in UpdatePlayerScoresAsync
    }
    
    public async Task UpdatePlayerScoresAsync(TournamentPlayer player, TournamentMatch match, Tournament tournament)
    {
        // Get the match result for this player's team
        var teamResult = match.Results.FirstOrDefault(r => r.Team.Players.Contains(player));
        
        if (teamResult == null)
            return;
        
        // Determine if the player's team won or lost
        var isWin = teamResult.GoalsWon > teamResult.GoalsLost;
        var isLoss = teamResult.GoalsWon < teamResult.GoalsLost;
        
        // In WinCount system, Score = WinCount
        // But we also consider goal difference for tie-breaking
        
        if (isWin)
        {
            player.WinCount++;
        }
        else if (isLoss)
        {
            player.LoseCount++;
        }
        
        // Update points
        player.PointsWon += teamResult.GoalsWon;
        player.PointsLost += teamResult.GoalsLost;
        player.MatchCount++;
        
        // Score is based on win count, with goal difference as tie-breaker
        // For display purposes, we'll use WinCount as the primary score
        var oldScore = player.Score;
        player.Score = player.WinCount + (player.PointsWon - player.PointsLost) * 0.01;
        player.ScoreDiff = player.Score - oldScore;
    }
}
