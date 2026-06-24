using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class PlanMatchesCommandHandler : IRequestHandler<PlanMatchesCommand, List<Guid>>
{
    private readonly ApplicationDbContext _context;
    private readonly Random _random = new Random();
    
    public PlanMatchesCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Guid>> Handle(PlanMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Teams)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        
        if (tournament == null)
        {
            throw new Exception("Tournament not found");
        }
        
        // Get all players in the tournament
        var players = tournament.TournamentPlayers.ToList();
        
        if (players.Count < 2)
        {
            throw new Exception("Need at least 2 players to plan matches");
        }
        
        // Get the team size (default to 2)
        var teamSize = tournament.TeamSize;
        
        // Calculate how many matches we need
        // Each match has teamSize * 2 players
        // Total player-games needed = players.Count * request.GamesPerPlayer
        // Each match provides teamSize * 2 player-games
        // So total matches = ceil((players.Count * request.GamesPerPlayer) / (teamSize * 2))
        // But we also need to account for the fact that players can't play against themselves
        
        // For simplicity, let's calculate the number of unique pairings we can create
        // and then multiply by the games per player
        
        // If FixedTeam is true, we create teams once and reuse them
        // If FixedTeam is false, we reshuffle teams for each match
        
        var matchIds = new List<Guid>();
        
        // Determine seeding source
        List<TournamentPlayer> seededPlayers = new();
        if (request.SeedTournamentId.HasValue && request.SeedTournamentId != Guid.Empty)
        {
            var seedTournament = await _context.Tournaments
                .Include(t => t.TournamentPlayers)
                    .ThenInclude(tp => tp.User)
                .FirstOrDefaultAsync(t => t.Id == request.SeedTournamentId, cancellationToken);
            
            if (seedTournament != null)
            {
                seededPlayers = seedTournament.TournamentPlayers
                    .OrderByDescending(tp => tp.Score)
                    .ToList();
            }
        }
        
        // If we don't have seeded players or no seed tournament, use current tournament's players
        if (!seededPlayers.Any())
        {
            seededPlayers = players.OrderByDescending(tp => tp.Score).ToList();
        }
        
        // Generate matches based on seeding type
        var existingMatchPlayerIds = tournament.Matches
            .SelectMany(m => m.Teams)
            .SelectMany(t => t.Players)
            .Select(p => p.Id)
            .ToHashSet();
        
        // Calculate how many matches to create
        // Each player should play GamesPerPlayer matches
        // Each match involves (teamSize * 2) players
        // Total required match-slots = (players.Count * GamesPerPlayer) / (teamSize * 2)
        int totalRequiredMatches = (int)Math.Ceiling(
            (players.Count * (double)request.GamesPerPlayer) / (teamSize * 2));
        
        // Make sure we have at least some matches
        totalRequiredMatches = Math.Max(totalRequiredMatches, players.Count / (teamSize * 2));
        totalRequiredMatches = Math.Max(totalRequiredMatches, 1);
        
        // Limit to a reasonable number
        totalRequiredMatches = Math.Min(totalRequiredMatches, 100);
        
        for (int i = 0; i < totalRequiredMatches; i++)
        {
            // Create teams for this match
            var (team1, team2) = CreateTeams(players, seededPlayers, request.SeedingType, request.FixedTeam, i, teamSize);
            
            // Create the match
            var match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                State = MatchState.Planned,
                Order = tournament.Matches.Count + 1 + i,
                CreatedAt = DateTime.UtcNow,
                Teams = new List<TournamentTeam> { team1, team2 }
            };
            
            team1.Tournament = tournament;
            team2.Tournament = tournament;
            team1.Matches = new List<TournamentMatch> { match };
            team2.Matches = new List<TournamentMatch> { match };
            
            _context.TournamentMatches.Add(match);
            matchIds.Add(match.Id);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        // Note: In a real implementation, we would publish MatchesPlannedEvent here
        
        return matchIds;
    }
    
    private (TournamentTeam team1, TournamentTeam team2) CreateTeams(
        List<TournamentPlayer> players,
        List<TournamentPlayer> seededPlayers,
        SeedingType seedingType,
        bool fixedTeam,
        int matchIndex,
        int teamSize)
    {
        var availablePlayers = new List<TournamentPlayer>(players);
        
        switch (seedingType)
        {
            case SeedingType.Random:
                return CreateRandomTeams(availablePlayers, teamSize, matchIndex);
            
            case SeedingType.Equality:
                return CreateEqualityTeams(availablePlayers, seededPlayers, teamSize);
            
            case SeedingType.Fair:
                return CreateFairTeams(availablePlayers, seededPlayers, teamSize);
            
            default:
                return CreateRandomTeams(availablePlayers, teamSize, matchIndex);
        }
    }
    
    private (TournamentTeam team1, TournamentTeam team2) CreateRandomTeams(
        List<TournamentPlayer> availablePlayers,
        int teamSize,
        int seed)
    {
        var shuffled = availablePlayers.OrderBy(x => _random.Next()).ToList();
        
        var team1Players = shuffled.Take(teamSize).ToList();
        var remaining = shuffled.Skip(teamSize).ToList();
        var team2Players = remaining.Take(teamSize).ToList();
        
        return CreateTeamsFromPlayers(team1Players, team2Players);
    }
    
    private (TournamentTeam team1, TournamentTeam team2) CreateEqualityTeams(
        List<TournamentPlayer> availablePlayers,
        List<TournamentPlayer> seededPlayers,
        int teamSize)
    {
        // Equality: pair best with worst (1+N, 2+(N-1), ...)
        // Use seededPlayers for ranking if available
        var rankedPlayers = seededPlayers.Any() ? seededPlayers : availablePlayers.OrderByDescending(p => p.Score).ToList();
        
        // Get only available players
        var rankedAvailable = rankedPlayers.Where(p => availablePlayers.Contains(p)).ToList();
        
        if (rankedAvailable.Count < teamSize * 2)
        {
            // Not enough players, fall back to random
            return CreateRandomTeams(availablePlayers, teamSize, 0);
        }
        
        // Pair best with worst
        var team1Players = new List<TournamentPlayer>();
        var team2Players = new List<TournamentPlayer>();
        
        for (int i = 0; i < teamSize; i++)
        {
            if (i < rankedAvailable.Count / 2)
            {
                team1Players.Add(rankedAvailable[i]);
            }
            if (i + rankedAvailable.Count / 2 < rankedAvailable.Count)
            {
                team2Players.Add(rankedAvailable[i + rankedAvailable.Count / 2]);
            }
        }
        
        // If we couldn't get enough players, fill with random
        while (team1Players.Count < teamSize && team2Players.Count < teamSize)
        {
            var remaining = availablePlayers.Except(team1Players.Concat(team2Players)).ToList();
            if (!remaining.Any()) break;
            
            team1Players.Add(remaining.First());
            remaining.RemoveAt(0);
            if (remaining.Any())
            {
                team2Players.Add(remaining.First());
            }
        }
        
        return CreateTeamsFromPlayers(team1Players, team2Players);
    }
    
    private (TournamentTeam team1, TournamentTeam team2) CreateFairTeams(
        List<TournamentPlayer> availablePlayers,
        List<TournamentPlayer> seededPlayers,
        int teamSize)
    {
        // Fair: split ranked players into top half and bottom half
        // Then pair best of top with best of bottom, etc.
        // Example: 10 players -> 1+6, 2+7, 3+8, 4+9, 5+10
        
        var rankedPlayers = seededPlayers.Any() ? seededPlayers : availablePlayers.OrderByDescending(p => p.Score).ToList();
        var rankedAvailable = rankedPlayers.Where(p => availablePlayers.Contains(p)).ToList();
        
        if (rankedAvailable.Count < teamSize * 2)
        {
            return CreateRandomTeams(availablePlayers, teamSize, 0);
        }
        
        // Split into top and bottom halves
        var topHalf = rankedAvailable.Take(rankedAvailable.Count / 2).ToList();
        var bottomHalf = rankedAvailable.Skip(rankedAvailable.Count / 2).ToList();
        
        // Pair best of top with best of bottom, etc.
        var team1Players = new List<TournamentPlayer>();
        var team2Players = new List<TournamentPlayer>();
        
        for (int i = 0; i < teamSize; i++)
        {
            if (i < topHalf.Count)
            {
                team1Players.Add(topHalf[i]);
            }
            if (i < bottomHalf.Count)
            {
                team2Players.Add(bottomHalf[i]);
            }
        }
        
        // Fill remaining spots with random players
        while (team1Players.Count < teamSize || team2Players.Count < teamSize)
        {
            var remaining = availablePlayers.Except(team1Players.Concat(team2Players)).ToList();
            if (!remaining.Any()) break;
            
            if (team1Players.Count < teamSize)
            {
                team1Players.Add(remaining.First());
                remaining.RemoveAt(0);
            }
            if (team2Players.Count < teamSize && remaining.Any())
            {
                team2Players.Add(remaining.First());
            }
        }
        
        return CreateTeamsFromPlayers(team1Players, team2Players);
    }
    
    private (TournamentTeam team1, TournamentTeam team2) CreateTeamsFromPlayers(
        List<TournamentPlayer> team1Players,
        List<TournamentPlayer> team2Players)
    {
        var team1 = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            Name = "Team 1",
            Number = 1,
            Players = team1Players
        };
        
        var team2 = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            Name = "Team 2",
            Number = 2,
            Players = team2Players
        };
        
        return (team1, team2);
    }
}
