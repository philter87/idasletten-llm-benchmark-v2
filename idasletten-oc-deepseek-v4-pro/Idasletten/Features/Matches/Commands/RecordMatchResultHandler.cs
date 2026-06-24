using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Scoring;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class RecordMatchResultHandler : IRequestHandler<RecordMatchResultCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public RecordMatchResultHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(RecordMatchResultCommand command, CancellationToken ct)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == command.TournamentId, ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var tournamentId = command.TournamentId;
        var match = command.MatchId.HasValue
            ? await _db.TournamentMatches
                .Include(m => m.TeamResults).ThenInclude(r => r.Team).ThenInclude(t => t.TeamPlayers)
                .FirstOrDefaultAsync(m => m.Id == command.MatchId.Value, ct)
            : null;

        if (match != null)
        {
            _db.TournamentTeamMatchResults.RemoveRange(match.TeamResults);
        }
        else
        {
            match = new TournamentMatch
            {
                Id = command.MatchId ?? Guid.NewGuid(),
                TournamentId = tournamentId,
                Order = 0,
                State = MatchState.Done,
                CreatedAt = DateTime.UtcNow,
                PlayedAt = DateTime.UtcNow
            };
            _db.TournamentMatches.Add(match);
        }

        var team1Players = await GetOrCreatePlayers(tournament, command.Team1Player1Initials, command.Team1Player2Initials, ct);
        var team2Players = await GetOrCreatePlayers(tournament, command.Team2Player1Initials, command.Team2Player2Initials, ct);

        var team1 = await CreateTeam(tournamentId, "Team 1", 1, team1Players, ct);
        var team2 = await CreateTeam(tournamentId, "Team 2", 2, team2Players, ct);

        var teamCount = _db.TournamentTeams.Count(t => t.TournamentId == tournamentId);
        team1.Number = teamCount + 1;
        team2.Number = teamCount + 2;

        var result1 = new TournamentTeamMatchResult
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            TournamentId = tournamentId,
            TeamId = team1.Id,
            GoalsWon = command.Team1Goals,
            GoalsLost = command.Team2Goals
        };
        var result2 = new TournamentTeamMatchResult
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            TournamentId = tournamentId,
            TeamId = team2.Id,
            GoalsWon = command.Team2Goals,
            GoalsLost = command.Team1Goals
        };

        _db.TournamentTeamMatchResults.AddRange(result1, result2);

        match.State = MatchState.Done;
        match.PlayedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Recalculate scores
        var allTeamPlayers = await _db.TournamentTeams
            .Include(t => t.TeamPlayers)
            .Where(t => t.Id == team1.Id || t.Id == team2.Id)
            .ToListAsync(ct);

        var playerIds = allTeamPlayers.SelectMany(t => t.TeamPlayers).Select(tp => tp.TournamentPlayerId).Distinct();
        var players = await _db.TournamentPlayers.Where(p => playerIds.Contains(p.Id)).ToListAsync(ct);

        var scoring = ScoringServiceFactory.Create(tournament.ScoreSystem);
        scoring.Calculate(match, [result1, result2], allTeamPlayers, players);

        foreach (var p in players)
        {
            p.MatchCount++;
            if (match.State == MatchState.Done)
            {
                var result = result1.TeamId == allTeamPlayers.First(t => t.TeamPlayers.Any(tp => tp.TournamentPlayerId == p.Id)).Id
                    ? result1 : result2;
                var won = result.GoalsWon > result.GoalsLost;
                if (won) p.WinCount++; else p.LoseCount++;
                p.PointsWon += result.GoalsWon;
                p.PointsLost += result.GoalsLost;
            }
        }

        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new MatchResultRecorded(match.Id, tournamentId), ct);
        return match.Id;
    }

    private async Task<List<TournamentPlayer>> GetOrCreatePlayers(
        Tournament tournament,
        string initials1,
        string? initials2,
        CancellationToken ct)
    {
        var players = new List<TournamentPlayer>();

        var p1 = await EnsurePlayer(tournament, initials1, ct);
        players.Add(p1);

        if (!string.IsNullOrWhiteSpace(initials2))
        {
            var p2 = await EnsurePlayer(tournament, initials2, ct);
            if (p2.Id != p1.Id)
                players.Add(p2);
        }

        return players;
    }

    private async Task<TournamentPlayer> EnsurePlayer(Tournament tournament, string initials, CancellationToken ct)
    {
        var user = await _mediator.Send(new CreateUserCommand(initials), ct);

        var player = tournament.Players.FirstOrDefault(p => p.UserId == user.Id);
        if (player != null) return player;

        player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TournamentId = tournament.Id,
            Score = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 1000,
            Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0
        };

        _db.TournamentPlayers.Add(player);
        tournament.Players.Add(player);
        return player;
    }

    private async Task<TournamentTeam> CreateTeam(Guid tournamentId, string name, int number,
        List<TournamentPlayer> teamPlayers, CancellationToken ct)
    {
        var team = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Name = name,
            Number = number
        };
        _db.TournamentTeams.Add(team);

        foreach (var tp in teamPlayers)
        {
            _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
            {
                TournamentTeamId = team.Id,
                TournamentPlayerId = tp.Id
            });
        }

        await _db.SaveChangesAsync(ct);
        return team;
    }
}
