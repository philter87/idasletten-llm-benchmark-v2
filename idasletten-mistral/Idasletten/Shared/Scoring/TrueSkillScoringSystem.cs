using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// TrueSkill scoring system implementation.
/// Note: This is a simplified implementation that uses Elo-style calculations.
/// For full TrueSkill calculations, the Moserware.Skills library would need .NET 8 compatibility.
/// </summary>
public class TrueSkillScoringSystem : ITrueSkillScoringSystem
{
    public double Mu { get; } = 25.0;
    public double Sigma { get; } = 8.333;
    
    private const double KFactor = 32.0;

    public async Task CalculateMatchResultsAsync(TournamentMatch match, Tournament tournament)
    {
        // Match-level calculations can be done here if needed
    }

    public async Task UpdatePlayerScoresAsync(TournamentPlayer player, TournamentMatch match, Tournament tournament)
    {
        var (winningTeam, losingTeam, isDraw) = CreateTeamsFromMatch(match);
        
        if (isDraw)
        {
            player.Score += 5.0;
            player.ScoreDiff = 5.0;
            player.WinCount++;
        }
        else if (winningTeam.Contains(player))
        {
            double expectedScore = CalculateExpectedScore(player.Score, CalculateAverageOpponentScore(losingTeam));
            double scoreChange = KFactor * (1.0 - expectedScore);
            
            player.ScoreDiff = scoreChange;
            player.Score += scoreChange;
            player.WinCount++;
        }
        else if (losingTeam.Contains(player))
        {
            double expectedScore = CalculateExpectedScore(player.Score, CalculateAverageOpponentScore(winningTeam));
            double scoreChange = KFactor * (0.0 - expectedScore);
            
            player.ScoreDiff = scoreChange;
            player.Score += scoreChange;
            player.LoseCount++;
        }
        
        player.MatchCount++;
        
        foreach (var result in match.Results)
        {
            foreach (var tp in result.Team?.Players ?? new List<TournamentPlayer>())
            {
                if (tp.Id == player.Id)
                {
                    tp.PointsWon += result.GoalsWon;
                    tp.PointsLost += result.GoalsLost;
                    break;
                }
            }
        }
    }

    private double CalculateExpectedScore(double playerScore, double opponentScore)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (opponentScore - playerScore) / 400.0));
    }

    private double CalculateAverageOpponentScore(List<TournamentPlayer> opponents)
    {
        if (opponents.Count == 0) return 1500.0;
        return opponents.Average(p => p.Score);
    }

    private (List<TournamentPlayer>, List<TournamentPlayer>, bool) CreateTeamsFromMatch(TournamentMatch match)
    {
        var winningTeam = new List<TournamentPlayer>();
        var losingTeam = new List<TournamentPlayer>();
        
        var results = match.Results?.ToList() ?? new List<TournamentTeamMatchResult>();
        if (results.Count < 2)
            return (winningTeam, losingTeam, true);
        
        var maxGoals = results.Max(r => r.GoalsWon);
        var winningResults = results.Where(r => r.GoalsWon == maxGoals).ToList();
        
        var isDraw = winningResults.Count > 1 || results.All(r => r.GoalsWon == r.GoalsLost);
        
        if (isDraw)
        {
            foreach (var result in results)
            {
                if (result.Team != null)
                {
                    winningTeam.AddRange(result.Team.Players);
                }
            }
            return (winningTeam, losingTeam, true);
        }
        
        foreach (var result in results)
        {
            if (result.Team != null)
            {
                if (result.GoalsWon == maxGoals)
                {
                    winningTeam.AddRange(result.Team.Players);
                }
                else
                {
                    losingTeam.AddRange(result.Team.Players);
                }
            }
        }
        
        return (winningTeam, losingTeam, false);
    }
}
