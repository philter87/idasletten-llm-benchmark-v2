using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Moserware.Skills;

namespace Idasletten.Shared.Scoring;

public class TrueSkillScoringSystem : ITrueSkillScoringSystem
{
    // Aggressive parameters as per user decision
    public double Mu { get; } = 30.0;
    public double Sigma { get; } = 10.0;
    
    private readonly double _drawProbability = 0.0; // No draws in foosball (first to PointsToWin)
    private readonly double _beta = 4.0; // Performance variance
    private readonly double _tau = Sigma / 100.0; // Dynamics factor
    
    private GameInfo _gameInfo;
    
    public TrueSkillScoringSystem()
    {
        // Create game info with TrueSkill parameters
        _gameInfo = new GameInfo(
            initialMean: Mu,
            initialStandardDeviation: Sigma,
            beta: _beta,
            tau: _tau,
            drawProbability: _drawProbability);
    }
    
    public async Task CalculateMatchResultsAsync(TournamentMatch match, Tournament tournament)
    {
        // TrueSkill calculation is handled per-player in UpdatePlayerScoresAsync
        // This method can be used for match-level calculations if needed
    }
    
    public async Task UpdatePlayerScoresAsync(TournamentPlayer player, TournamentMatch match, Tournament tournament)
    {
        // Create teams and their ratings
        var (winningTeam, losingTeam, isDraw) = CreateTeamsFromMatch(match);
        
        // Create ratings for each player
        var teamRatings = new List<TeamRating>();
        
        // Add winning team
        if (winningTeam.Any())
        {
            var winningTeamRatings = new TeamRating(winningTeam);
            foreach (var p in winningTeam)
            {
                var rating = new Rating(p.Score, Sigma);
                winningTeamRatings.AddPlayer(rating);
            }
            teamRatings.Add(winningTeamRatings);
        }
        
        // Add losing team
        if (losingTeam.Any())
        {
            var losingTeamRatings = new TeamRating(losingTeam);
            foreach (var p in losingTeam)
            {
                var rating = new Rating(p.Score, Sigma);
                losingTeamRatings.AddPlayer(rating);
            }
            teamRatings.Add(losingTeamRatings);
        }
        
        // Calculate new ratings based on match outcome
        var newRatings = _gameInfo.CalculateNewRatings(
            _gameInfo.CreateNewGameRatingUpdate(
                teamRatings,
                isDraw ? GameResult.Draw : GameResult.FirstTeamWin));
        
        // Update player scores
        for (int i = 0; i < winningTeam.Count; i++)
        {
            var team = winningTeam;
            var oldRating = new Ratings(team[i].Score, Sigma);
            var newRating = newRatings[i];
            
            team[i].ScoreDiff = newRating.Mean - oldRating.Mean;
            team[i].Score = newRating.Mean;
            team[i].WinCount++;
            team[i].MatchCount++;
        }
        
        for (int i = 0; i < losingTeam.Count; i++)
        {
            var team = losingTeam;
            var oldRating = new Ratings(team[i].Score, Sigma);
            var newRating = newRatings[winningTeam.Any() ? 1 : 0 + i];
            
            team[i].ScoreDiff = newRating.Mean - oldRating.Mean;
            team[i].Score = newRating.Mean;
            team[i].LoseCount++;
            team[i].MatchCount++;
        }
        
        // Update points from the match results
        foreach (var result in match.Results)
        {
            foreach (var tp in result.Team.Players)
            {
                tp.PointsWon += result.GoalsWon;
                tp.PointsLost += result.GoalsLost;
            }
        }
    }
    
    private (List<TournamentPlayer>, List<TournamentPlayer>, bool) CreateTeamsFromMatch(TournamentMatch match)
    {
        var winningTeam = new List<TournamentPlayer>();
        var losingTeam = new List<TournamentPlayer>();
        
        // Get all results and find the winner
        var results = match.Results.ToList();
        if (results.Count < 2)
            return (winningTeam, losingTeam, true);
        
        // Find the team with the most goals
        var maxGoals = results.Max(r => r.GoalsWon);
        var winningResults = results.Where(r => r.GoalsWon == maxGoals).ToList();
        
        // If multiple teams have the same max goals, it's a draw
        var isDraw = winningResults.Count > 1 || results.All(r => r.GoalsWon == r.GoalsLost);
        
        if (isDraw)
        {
            // In a draw, all teams are considered "winning" for TrueSkill purposes
            foreach (var result in results)
            {
                winningTeam.AddRange(result.Team.Players);
            }
            return (winningTeam, losingTeam, true);
        }
        
        // Separate into winning and losing teams
        foreach (var result in results)
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
        
        return (winningTeam, losingTeam, false);
    }
}
