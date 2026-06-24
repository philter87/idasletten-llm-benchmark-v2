using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Scoring;

public class LivesScoringSystem : ILivesScoringSystem
{
    public int InitialLives { get; set; } = 3;
    
    public async Task CalculateMatchResultsAsync(TournamentMatch match, Tournament tournament)
    {
        // Lives scoring is handled per-player in UpdatePlayerScoresAsync
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
        
        // In Lives system, losing a match means losing a life
        if (isLoss)
        {
            player.Lives--;
        }
        
        // Score is based on remaining lives
        player.Score = player.Lives;
        
        // Update win/loss counts
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
        
        // ScoreDiff is the change in lives
        player.ScoreDiff = isLoss ? -1 : 0;
    }
}
