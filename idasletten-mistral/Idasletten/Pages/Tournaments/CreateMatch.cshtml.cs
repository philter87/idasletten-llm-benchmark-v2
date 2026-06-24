using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IScoringSystemFactory _scoringSystemFactory;
    
    public CreateMatchModel(ApplicationDbContext context, IScoringSystemFactory scoringSystemFactory)
    {
        _context = context;
        _scoringSystemFactory = scoringSystemFactory;
    }
    
    public Tournament Tournament { get; set; } = default!;
    public Guid? MatchId { get; set; }
    public TournamentMatch? ExistingMatch { get; set; }
    
    // Form data
    [BindProperty]
    public List<string> TeamPlayers { get; set; } = new List<string>();
    
    [BindProperty]
    public List<int> TeamGoals { get; set; } = new List<int>();
    
    [BindProperty]
    public MatchState MatchState { get; set; } = MatchState.Planned;
    
    // For custom initials
    public List<string> CustomInitials { get; set; } = new List<string>();
    
    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? matchId = null)
    {
        Tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        
        if (Tournament == null)
        {
            return NotFound();
        }
        
        MatchId = matchId;
        
        if (matchId.HasValue)
        {
            ExistingMatch = await _context.TournamentMatches
                .Include(m => m.Teams)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(p => p.User)
                .Include(m => m.Results)
                    .ThenInclude(r => r.Team)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);
            
            if (ExistingMatch == null)
            {
                return NotFound();
            }
            
            // Pre-fill form data from existing match
            InitializeFormData();
        }
        else
        {
            // Initialize empty form data
            for (int i = 0; i < Tournament.TeamSize * 2; i++)
            {
                TeamPlayers.Add(string.Empty);
                TeamGoals.Add(0);
                CustomInitials.Add(string.Empty);
            }
        }
        
        return Page();
    }
    
    private void InitializeFormData()
    {
        if (ExistingMatch == null) return;
        
        // Get all players in the match ordered by team
        var matchPlayers = ExistingMatch.Teams
            .OrderBy(t => t.Number)
            .SelectMany(t => t.Players.OrderBy(p => p.User.UserName))
            .Select(p => p.User.UserName)
            .ToList();
        
        // Get goals for each team
        var matchGoals = ExistingMatch.Results
            .OrderBy(r => r.Team.Number)
            .Select(r => r.GoalsWon)
            .ToList();
        
        // Initialize form data
        TeamPlayers = new List<string>();
        TeamGoals = new List<int>();
        CustomInitials = new List<string>();
        
        for (int i = 0; i < Tournament.TeamSize * 2; i++)
        {
            if (i < matchPlayers.Count)
            {
                TeamPlayers.Add(matchPlayers[i]);
                CustomInitials.Add(string.Empty);
            }
            else
            {
                TeamPlayers.Add(string.Empty);
                CustomInitials.Add(string.Empty);
            }
            
            if (i < 2)
            {
                TeamGoals.Add(i < matchGoals.Count ? matchGoals[i] : 0);
            }
            else
            {
                TeamGoals.Add(0);
            }
        }
        
        MatchState = ExistingMatch.State;
    }
    
    public async Task<IActionResult> OnPostAsync(Guid tournamentId, Guid? matchId, List<string> teamPlayers, 
        List<int> teamGoals, List<string> teamPlayersCustom, MatchState matchState = MatchState.Planned)
    {
        Tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        
        if (Tournament == null)
        {
            return NotFound();
        }
        
        // Check login requirement for editing completed matches
        if (matchId.HasValue)
        {
            var existingMatch = await _context.TournamentMatches
                .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);
            
            if (existingMatch?.State == MatchState.Done && !User.Identity.IsAuthenticated)
            {
                return Forbid();
            }
        }
        
        // Create or update the match
        TournamentMatch match;
        
        if (matchId.HasValue && matchId != Guid.Empty)
        {
            match = await _context.TournamentMatches
                .Include(m => m.Teams)
                .Include(m => m.Results)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);
            
            if (match == null)
            {
                return NotFound();
            }
        }
        else
        {
            match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                Order = (await _context.TournamentMatches.CountAsync(m => m.TournamentId == tournamentId)) + 1,
                State = matchState,
                CreatedAt = DateTime.UtcNow
            };
            _context.TournamentMatches.Add(match);
        }
        
        // Clear existing teams and results
        if (match.Teams.Any())
        {
            foreach (var team in match.Teams.ToList())
            {
                _context.TournamentTeams.Remove(team);
            }
        }
        if (match.Results.Any())
        {
            foreach (var result in match.Results.ToList())
            {
                _context.TournamentTeamMatchResults.Remove(result);
            }
        }
        
        // Build team structure
        var teams = new List<TournamentTeam>();
        var teamMap = new Dictionary<int, TournamentTeam>();
        
        // For simplicity, we'll create 2 teams (TeamSize players each)
        // Team 1: Players 0 to TeamSize-1
        // Team 2: Players TeamSize to TeamSize*2-1
        
        for (int teamNum = 0; teamNum < 2; teamNum++)
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                Name = teamNum == 0 ? "Team 1" : "Team 2",
                Number = teamNum + 1,
                TournamentId = tournamentId
            };
            teams.Add(team);
            teamMap[teamNum] = team;
            _context.TournamentTeams.Add(team);
        }
        
        // Add players to teams and create results
        var allPlayers = new List<TournamentPlayer>();
        
        for (int i = 0; i < Tournament.TeamSize * 2; i++)
        {
            var initials = teamPlayers[i];
            var customInitials = teamPlayersCustom[i];
            var playerInitials = string.IsNullOrEmpty(customInitials) ? initials : customInitials;
            
            if (string.IsNullOrEmpty(playerInitials))
            {
                continue; // Skip empty players
            }
            
            // Find or create user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == playerInitials);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = playerInitials,
                    NormalizedUserName = playerInitials.ToUpper(),
                    Email = $"{playerInitials.ToLower()}@idasletten.local",
                    NormalizedEmail = $"{playerInitials.ToLower()}@IDASLETTEN.LOCAL"
                };
                _context.Users.Add(user);
            }
            
            // Find or create tournament player
            var tournamentPlayer = await _context.TournamentPlayers
                .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == tournamentId);
            
            if (tournamentPlayer == null)
            {
                tournamentPlayer = new TournamentPlayer
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TournamentId = tournamentId,
                    Score = Tournament.ScoreSystem == ScoreSystem.Lives ? Tournament.TeamSize * 3 : 1500.0
                };
                _context.TournamentPlayers.Add(tournamentPlayer);
            }
            
            allPlayers.Add(tournamentPlayer);
        }
        
        // Assign players to teams
        for (int i = 0; i < allPlayers.Count; i++)
        {
            int teamIndex = i < Tournament.TeamSize ? 0 : 1;
            if (teamMap.TryGetValue(teamIndex, out var team))
            {
                team.Players.Add(allPlayers[i]);
            }
        }
        
        // Create results
        var totalTeams = teams.Count;
        for (int i = 0; i < totalTeams; i++)
        {
            var goals = i < teamGoals.Count ? teamGoals[i] : 0;
            
            var result = new TournamentTeamMatchResult
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = tournamentId,
                TeamId = teams[i].Id,
                GoalsWon = goals,
                GoalsLost = 0 // Will be calculated
            };
            match.Results.Add(result);
            _context.TournamentTeamMatchResults.Add(result);
        }
        
        // Set goals lost for each team
        for (int i = 0; i < match.Results.Count; i++)
        {
            var otherTeamIndex = i == 0 ? 1 : 0;
            if (i < teamGoals.Count && otherTeamIndex < teamGoals.Count)
            {
                match.Results[i].GoalsLost = teamGoals[otherTeamIndex];
            }
        }
        
        // Update match state
        match.State = matchState;
        
        if (matchState == MatchState.Done)
        {
            match.CompletedAt = DateTime.UtcNow;
            
            // Update player scores based on the scoring system
            await UpdatePlayerScoresAsync(match, Tournament);
        }
        
        match.Teams = teams;
        match.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return RedirectToPage("/Tournaments/Matches", new { id = tournamentId });
    }
    
    private async Task UpdatePlayerScoresAsync(TournamentMatch match, Tournament tournament)
    {
        var scoringSystem = _scoringSystemFactory.GetScoringSystem(tournament.ScoreSystem);
        
        foreach (var team in match.Teams)
        {
            foreach (var player in team.Players)
            {
                await scoringSystem.UpdatePlayerScoresAsync(player, match, tournament);
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    // Helper methods for the view
    public bool GetSelectedPlayer(int teamNum, string initials)
    {
        if (TeamPlayers.Count >= teamNum)
        {
            return TeamPlayers[teamNum - 1] == initials;
        }
        return false;
    }
    
    public string GetCustomInitials(int teamNum)
    {
        if (CustomInitials.Count >= teamNum)
        {
            return CustomInitials[teamNum - 1];
        }
        return string.Empty;
    }
    
    public string GetPlayerName(int teamNum)
    {
        if (TeamPlayers.Count >= teamNum && !string.IsNullOrEmpty(TeamPlayers[teamNum - 1]))
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == TeamPlayers[teamNum - 1]);
            if (user != null)
            {
                return user.Name ?? string.Empty;
            }
        }
        return string.Empty;
    }
    
    public int GetTeamGoals(int teamNum)
    {
        if (TeamGoals.Count >= teamNum)
        {
            return TeamGoals[teamNum - 1];
        }
        return 0;
    }
}
