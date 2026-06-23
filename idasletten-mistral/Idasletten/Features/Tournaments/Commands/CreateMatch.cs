using Idasletten.Features.Users;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Idasletten.Features.Tournaments.Commands;

public class CreateMatchCommand : IRequest<Guid>
{
    public Guid TournamentId { get; set; }
    public Guid MatchId { get; set; } = Guid.Empty;
    public string[]? Team1Initials { get; set; }
    public string[]? Team2Initials { get; set; }
    public string[]? Team3Initials { get; set; }
    public string[]? Team4Initials { get; set; }
    public int Team1Goals { get; set; }
    public int Team2Goals { get; set; }
    public bool OverwriteCompletedMatch { get; set; }
}

public class CreateMatchHandler : IRequestHandler<CreateMatchCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;
    private readonly UserManager<User> _userManager;

    public CreateMatchHandler(
        AppDbContext context,
        IPublisher publisher,
        UserManager<User> userManager)
    {
        _context = context;
        _publisher = publisher;
        _userManager = userManager;
    }

    public async Task<Guid> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
    {
        // Get tournament
        var tournament = await _context.Tournaments
            .Include(t => t.Players)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Teams)
                    .ThenInclude(tt => tt.Players)
                        .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament == null)
        {
            throw new ArgumentException("Turnering findes ikke");
        }

        // Get existing match if editing
        TournamentMatch? existingMatch = null;
        if (request.MatchId != Guid.Empty)
        {
            existingMatch = await _context.TournamentMatches
                .Include(m => m.Teams)
                    .ThenInclude(tt => tt.Players)
                        .ThenInclude(tp => tp.User)
                .Include(m => m.Results)
                .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

            if (existingMatch == null || existingMatch.TournamentId != tournament.Id)
            {
                throw new ArgumentException("Kamp findes ikke");
            }
        }

        // Validate that we can edit a completed match
        if (existingMatch?.State == MatchState.Done && !request.OverwriteCompletedMatch)
        {
            throw new ArgumentException("Du kan ikke redigere en afsluttet kamp uden tilladelse");
        }

        // Get next match order
        var nextOrder = tournament.Matches.Count + 1;
        if (existingMatch != null)
        {
            nextOrder = existingMatch.Order;
        }

        // Create or update match
        var match = existingMatch ?? new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Order = nextOrder,
            State = MatchState.Planned
        };

        // Create teams from initials
        var allInitials = new List<string[]> { request.Team1Initials, request.Team2Initials };
        if (request.Team3Initials != null) allInitials.Add(request.Team3Initials);
        if (request.Team4Initials != null) allInitials.Add(request.Team4Initials);

        var teamIndex = 0;
        var teamGoals = new List<int> { request.Team1Goals, request.Team2Goals };

        // Clear existing teams if editing
        if (existingMatch != null)
        {
            _context.TournamentTeamMatchResults.RemoveRange(existingMatch.Results);
            
            // Clear team-match relationships
            foreach (var team in existingMatch.Teams)
            {
                team.Matches.Remove(existingMatch);
            }
            existingMatch.Teams.Clear();
        }

        foreach (var teamInitials in allInitials.Where(t => t != null && t.Any()))
        {
            teamIndex++;
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                Name = "Hold " + teamIndex,
                Number = teamIndex,
                TournamentId = tournament.Id
            };

            // Add players from initials
            foreach (var initials in teamInitials!)
            {
                if (string.IsNullOrWhiteSpace(initials)) continue;

                var cleanInitials = initials.Trim().ToUpper();
                if (cleanInitials.Length != 3) continue;

                // Find or create user
                var user = await _userManager.FindByNameAsync(cleanInitials);
                if (user == null)
                {
                    user = new User
                    {
                        UserName = cleanInitials,
                        Name = cleanInitials,
                        Email = cleanInitials + "@example.com",
                        EmailConfirmed = false
                    };
                    
                    await _userManager.CreateAsync(user);
                }

                // Find or create tournament player
                var tournamentPlayer = tournament.Players.FirstOrDefault(tp => tp.UserId == user.Id);
                if (tournamentPlayer == null)
                {
                    tournamentPlayer = new TournamentPlayer
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        TournamentId = tournament.Id,
                        Score = tournament.ScoreSystem == ScoreSystem.Lives ? tournament.Players.Any() ? tournament.Players.Max(tp => tp.Score) : 1000 : 0,
                        Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 3
                    };
                    tournament.Players.Add(tournamentPlayer);
                }

                team.Players.Add(tournamentPlayer);
                tournamentPlayer.Teams.Add(team);
            }

            // Add team to match
            match.Teams.Add(team);
            team.Matches.Add(match);
        }

        // Determine match state based on goals
        var totalGoalsTeam1 = request.Team1Goals;
        var totalGoalsTeam2 = request.Team2Goals;
        
        var hasWon = totalGoalsTeam1 >= tournament.PointsToWin || totalGoalsTeam2 >= tournament.PointsToWin;
        match.State = hasWon ? MatchState.Done : MatchState.Planned;

        // Save match
        if (existingMatch == null)
        {
            _context.TournamentMatches.Add(match);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Create match results
        var teamsInMatch = match.Teams.OrderBy(t => t.Number).ToList();
        
        // Create results for each team
        var goals = new List<int> { request.Team1Goals, request.Team2Goals };
        for (int i = 0; i < Math.Min(teamsInMatch.Count, goals.Count); i++)
        {
            var result = new TournamentTeamMatchResult
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TeamId = teamsInMatch[i].Id,
                GoalsWon = goals[i],
                GoalsLost = i == 0 ? (teamsInMatch.Count > 1 ? goals[1] : 0) : goals[0]
            };
            match.Results.Add(result);
        }

        // If match is done, update player scores based on tournament scoring system
        if (match.State == MatchState.Done)
        {
            await UpdatePlayerScores(match, tournament, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Publish event
        await _publisher.Publish(new MatchCreated(match.Id, match.TournamentId), cancellationToken);

        return match.Id;
    }

    private async Task UpdatePlayerScores(TournamentMatch match, Tournament tournament, CancellationToken cancellationToken)
    {
        // This will be implemented based on the scoring system
        // For now, implement basic WinCount and Lives systems
        
        var results = match.Results.ToList();
        
        foreach (var team in match.Teams)
        {
            var teamResult = results.FirstOrDefault(r => r.TeamId == team.Id);
            if (teamResult == null) continue;

            var isWinner = teamResult.GoalsWon >= tournament.PointsToWin;

            foreach (var player in team.Players)
            {
                // Update player stats
                player.MatchCount++;
                
                if (isWinner)
                {
                    player.WinCount++;
                    
                    switch (tournament.ScoreSystem)
                    {
                        case ScoreSystem.WinCount:
                            player.Score = player.WinCount;
                            break;
                        case ScoreSystem.Elo:
                            // Simple Elo implementation - winner gains 20 points
                            player.Score += 20;
                            player.ScoreDiff += 20;
                            break;
                        case ScoreSystem.TrueSkill:
                            // TrueSkill will be implemented separately
                            // For now, use similar to Elo
                            player.Score += 15;
                            player.ScoreDiff += 15;
                            break;
                        case ScoreSystem.Lives:
                            // Winner keeps their lives, loser loses one
                            // For now, just update score based on wins
                            player.Score = player.WinCount * 100;
                            break;
                    }
                }
                else
                {
                    player.LoseCount++;
                    
                    switch (tournament.ScoreSystem)
                    {
                        case ScoreSystem.Elo:
                            // Loser loses 20 points
                            player.Score = Math.Max(0, player.Score - 20);
                            player.ScoreDiff -= 20;
                            break;
                        case ScoreSystem.TrueSkill:
                            player.Score = Math.Max(0, player.Score - 15);
                            player.ScoreDiff -= 15;
                            break;
                        case ScoreSystem.Lives:
                            // Loser loses a life
                            player.Lives = Math.Max(0, player.Lives - 1);
                            player.Score = player.WinCount * 100 + (player.Lives * 10);
                            break;
                        case ScoreSystem.WinCount:
                            // Score stays the same for losers
                            break;
                    }
                }

                // Update points won/lost
                player.PointsWon += teamResult.GoalsWon;
                player.PointsLost += teamResult.GoalsLost;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record MatchCreated(Guid MatchId, Guid TournamentId) : INotification;
