using Idasletten.Features.Scoring;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record RecordMatchResultCommand(
    Guid TournamentId,
    List<string> Team1Initials,
    List<string> Team2Initials,
    int Team1Goals,
    int Team2Goals,
    Guid? ExistingMatchId = null
) : IRequest<TournamentMatch>;

public class RecordMatchResultHandler : IRequestHandler<RecordMatchResultCommand, TournamentMatch>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public RecordMatchResultHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<TournamentMatch> Handle(RecordMatchResultCommand request, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var team1Players = await EnsurePlayersExist(request.Team1Initials, request.TournamentId, tournament, ct);
        var team2Players = await EnsurePlayersExist(request.Team2Initials, request.TournamentId, tournament, ct);

        // Create or update teams for this match
        var team1 = await CreateTeam(team1Players, request.TournamentId, 1, ct);
        var team2 = await CreateTeam(team2Players, request.TournamentId, 2, ct);

        TournamentMatch match;
        if (request.ExistingMatchId.HasValue)
        {
            match = await _db.TournamentMatches
                .Include(m => m.TeamResults)
                .FirstOrDefaultAsync(m => m.Id == request.ExistingMatchId.Value, ct)
                ?? throw new InvalidOperationException("Match not found");

            // Remove old results
            _db.TournamentTeamMatchResults.RemoveRange(match.TeamResults);
        }
        else
        {
            var maxOrder = await _db.TournamentMatches
                .Where(m => m.TournamentId == request.TournamentId)
                .MaxAsync(m => (int?)m.Order, ct) ?? 0;

            match = new TournamentMatch
            {
                TournamentId = request.TournamentId,
                Order = maxOrder + 1,
                State = MatchState.Done,
                PlayedAt = DateTime.UtcNow
            };
            _db.TournamentMatches.Add(match);
            await _db.SaveChangesAsync(ct);
        }

        match.State = MatchState.Done;
        match.PlayedAt = DateTime.UtcNow;

        _db.TournamentTeamMatchResults.AddRange(
            new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                TeamId = team1.Id,
                GoalsWon = request.Team1Goals,
                GoalsLost = request.Team2Goals
            },
            new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                TeamId = team2.Id,
                GoalsWon = request.Team2Goals,
                GoalsLost = request.Team1Goals
            }
        );

        // Calculate scores
        var scoringService = ScoringServiceFactory.Create(tournament.ScoreSystem);
        scoringService.CalculateScores(team1Players, team2Players, request.Team1Goals, request.Team2Goals, tournament);

        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new MatchResultRecorded(match.Id, request.TournamentId), ct);

        return match;
    }

    private async Task<List<TournamentPlayer>> EnsurePlayersExist(
        List<string> initials, Guid tournamentId, Tournament tournament, CancellationToken ct)
    {
        var players = new List<TournamentPlayer>();
        foreach (var initial in initials)
        {
            var username = initial.ToUpperInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
            if (user == null)
            {
                user = new User { Username = username, Name = username };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
            }

            var player = await _db.TournamentPlayers
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.TournamentId == tournamentId, ct);
            if (player == null)
            {
                player = new TournamentPlayer
                {
                    UserId = user.Id,
                    TournamentId = tournamentId,
                    Score = tournament.ScoreSystem == ScoreSystem.Elo ? 1000 : 0,
                    Lives = 3
                };
                _db.TournamentPlayers.Add(player);
                await _db.SaveChangesAsync(ct);
            }
            players.Add(player);
        }
        return players;
    }

    private async Task<TournamentTeam> CreateTeam(
        List<TournamentPlayer> players, Guid tournamentId, int teamNumber, CancellationToken ct)
    {
        var team = new TournamentTeam
        {
            TournamentId = tournamentId,
            Number = teamNumber,
            Name = $"Team {teamNumber}"
        };
        _db.TournamentTeams.Add(team);
        await _db.SaveChangesAsync(ct);

        foreach (var player in players)
        {
            _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
            {
                TournamentTeamId = team.Id,
                TournamentPlayerId = player.Id
            });
        }
        await _db.SaveChangesAsync(ct);
        return team;
    }
}

public record MatchResultRecorded(Guid MatchId, Guid TournamentId) : INotification;
