using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record CreatePlannedMatchCommand(
    Guid TournamentId,
    List<string> Team1Initials,
    List<string> Team2Initials
) : IRequest<TournamentMatch>;

public class CreatePlannedMatchHandler : IRequestHandler<CreatePlannedMatchCommand, TournamentMatch>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreatePlannedMatchHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<TournamentMatch> Handle(CreatePlannedMatchCommand request, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var team1Players = await EnsurePlayersExist(request.Team1Initials, request.TournamentId, tournament, ct);
        var team2Players = await EnsurePlayersExist(request.Team2Initials, request.TournamentId, tournament, ct);

        var team1 = await CreateTeam(team1Players, request.TournamentId, 1, ct);
        var team2 = await CreateTeam(team2Players, request.TournamentId, 2, ct);

        var maxOrder = await _db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .MaxAsync(m => (int?)m.Order, ct) ?? 0;

        var match = new TournamentMatch
        {
            TournamentId = request.TournamentId,
            Order = maxOrder + 1,
            State = MatchState.Planned
        };
        _db.TournamentMatches.Add(match);

        _db.TournamentTeamMatchResults.AddRange(
            new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                TeamId = team1.Id,
                GoalsWon = 0,
                GoalsLost = 0
            },
            new TournamentTeamMatchResult
            {
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                TeamId = team2.Id,
                GoalsWon = 0,
                GoalsLost = 0
            }
        );

        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new PlannedMatchCreated(match.Id, request.TournamentId), ct);

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

public record PlannedMatchCreated(Guid MatchId, Guid TournamentId) : INotification;
