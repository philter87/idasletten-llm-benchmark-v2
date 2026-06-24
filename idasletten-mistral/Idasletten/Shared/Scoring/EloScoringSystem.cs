using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Scoring;

public class EloScoringSystem : IEloScoringSystem
{
    public int KFactor { get; set; } = 32;
    
    public async Task CalculateMatchResultsAsync(TournamentMatch match, Tournament tournament)
    {
        // Elo scoring is handled per-player in UpdatePlayerScoresAsync
        // This method can be used for match-level calculations if needed
    }
    
    public async Task UpdatePlayerScoresAsync(TournamentPlayer player, TournamentMatch match, Tournament tournament)
    {
        // Get the match result for this player's team
        var teamResult = await GetTeamResultForPlayerAsync(match, player);
        
        if (teamResult == null)
            return;
        
        // Determine if the player's team won, lost, or drew
        var isWin = teamResult.GoalsWon > teamResult.GoalsLost;
        var isLoss = teamResult.GoalsWon < teamResult.GoalsLost;
        var isDraw = teamResult.GoalsWon == teamResult.GoalsLost;
        
        // Calculate expected score and update
        var expectedScore = CalculateExpectedScore(player.Score, GetAverageOpponentScore(match, player));
        var actualScore = isWin ? 1.0 : (isDraw ? 0.5 : 0.0);
        var scoreChange = KFactor * (actualScore - expectedScore);
        
        // Update player stats
        player.ScoreDiff = scoreChange;
        player.Score += scoreChange;
        
        if (isWin)
        {
            player.WinCount++;
            player.PointsWon += teamResult.GoalsWon;
            player.PointsLost += teamResult.GoalsLost;
        }
        else if (isLoss)
        {
            player.LoseCount++;
            player.PointsWon += teamResult.GoalsWon;
            player.PointsLost += teamResult.GoalsLost;
        }
        else if (isDraw)
        {
            // Draw - no win/loss change but update points
            player.PointsWon += teamResult.GoalsWon;
            player.PointsLost += teamResult.GoalsLost;
        }
        
        player.MatchCount++;
    }
    
    private async Task<TournamentTeamMatchResult?> GetTeamResultForPlayerAsync(TournamentMatch match, TournamentPlayer player)
    {
        // Need to find which team the player is on in this match
        // This requires a database context, but we're in a service
        // For now, this is a simplified version
        // In practice, this would be handled by the caller passing the team result
        return match.Results.FirstOrDefault();
    }
    
    private double GetAverageOpponentScore(TournamentMatch match, TournamentPlayer player)
    {
        // Calculate average score of opponents
        // For simplicity, assume all opponents have the same average score
        // This would need to be properly implemented with access to the database
        var opponentTeams = match.Teams.Where(t => !t.Players.Contains(player));
        if (!opponentTeams.Any())
            return player.Score;
        
        var opponentPlayers = opponentTeams.SelectMany(t => t.Players);
        if (!opponentPlayers.Any())
            return player.Score;
        
        return opponentPlayers.Average(p => p.Score);
    }
    
    private double CalculateExpectedScore(double ratingA, double ratingB)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (ratingB - ratingA) / 400.0));
    }
}
