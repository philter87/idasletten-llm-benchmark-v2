using Idasletten.Features.Matches.Entities;
using Idasletten.Features.Matches.Events;
using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Users.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record PlanMatchCommand(Guid TournamentId, IReadOnlyList<IReadOnlyList<string>> TeamPlayerInitials) : IRequest<Guid>;

public sealed class PlanMatchHandler(AppDbContext db, IMediator mediator) : IRequestHandler<PlanMatchCommand, Guid>
{
    private readonly AppDbContext _db = db;
    private readonly IMediator _mediator = mediator;
    private const int DefaultLives = 3;

    public async Task<Guid> Handle(PlanMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(value => value.Id == request.TournamentId, cancellationToken);

        if (tournament is null)
        {
            throw new InvalidOperationException($"Tournament '{request.TournamentId}' was not found.");
        }

        ValidateTeams(request.TeamPlayerInitials, tournament.TeamSize);

        var preparedTeams = request.TeamPlayerInitials
            .Select(team => team.Select(initial => NormalizeRequired(initial, nameof(request.TeamPlayerInitials))).ToList())
            .ToList();

        var duplicateNames = preparedTeams
            .SelectMany(team => team)
            .GroupBy(initial => initial, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            throw new InvalidOperationException($"A player may only appear once per match. Duplicates: {string.Join(", ", duplicateNames)}.");
        }

        var usernameKeys = preparedTeams
            .SelectMany(team => team)
            .Select(initial => initial.ToLowerInvariant())
            .Distinct()
            .ToList();

        var users = await _db.Users
            .Where(user => usernameKeys.Contains(user.Username.ToLower()))
            .ToListAsync(cancellationToken);

        var usersByKey = users.ToDictionary(user => user.Username.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

        foreach (var username in preparedTeams.SelectMany(team => team))
        {
            var key = username.ToLowerInvariant();
            if (usersByKey.ContainsKey(key))
            {
                continue;
            }

            var user = new User
            {
                Username = username,
                Name = username
            };

            _db.Users.Add(user);
            usersByKey[key] = user;
        }

        var userIds = usersByKey.Values.Select(user => user.Id).Distinct().ToList();
        var tournamentPlayers = await _db.TournamentPlayers
            .Where(player => player.TournamentId == tournament.Id && userIds.Contains(player.UserId))
            .ToListAsync(cancellationToken);

        var playersByUserId = tournamentPlayers.ToDictionary(player => player.UserId);

        foreach (var user in usersByKey.Values)
        {
            if (playersByUserId.ContainsKey(user.Id))
            {
                continue;
            }

            var tournamentPlayer = new TournamentPlayer
            {
                TournamentId = tournament.Id,
                UserId = user.Id,
                Score = GetInitialScore(tournament.ScoreSystem),
                Lives = DefaultLives
            };

            _db.TournamentPlayers.Add(tournamentPlayer);
            playersByUserId[user.Id] = tournamentPlayer;
        }

        var nextOrder = (await _db.TournamentMatches
            .Where(match => match.TournamentId == tournament.Id)
            .Select(match => (int?)match.Order)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var nextTeamNumber = (await _db.TournamentTeams
            .Where(team => team.TournamentId == tournament.Id)
            .Select(team => (int?)team.Number)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var match = new TournamentMatch
        {
            TournamentId = tournament.Id,
            Order = nextOrder,
            State = MatchState.Planned
        };

        _db.TournamentMatches.Add(match);

        foreach (var team in preparedTeams)
        {
            var teamEntity = new TournamentTeam
            {
                TournamentId = tournament.Id,
                Number = nextTeamNumber,
                Name = $"Team {nextTeamNumber}"
            };

            nextTeamNumber += 1;
            _db.TournamentTeams.Add(teamEntity);

            foreach (var username in team)
            {
                var user = usersByKey[username.ToLowerInvariant()];
                _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
                {
                    Team = teamEntity,
                    TournamentPlayerId = playersByUserId[user.Id].Id
                });
            }

            _db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
            {
                Match = match,
                TournamentId = tournament.Id,
                Team = teamEntity,
                GoalsWon = 0,
                GoalsLost = 0
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _mediator.Publish(new MatchPlanned(match.Id, tournament.Id), cancellationToken);

        return match.Id;
    }

    private static void ValidateTeams(IReadOnlyList<IReadOnlyList<string>> teams, int expectedTeamSize)
    {
        if (teams.Count != 2)
        {
            throw new InvalidOperationException("A planned match must contain exactly two teams.");
        }

        foreach (var team in teams)
        {
            if (team is null || team.Count != expectedTeamSize)
            {
                throw new InvalidOperationException($"Each team must contain exactly {expectedTeamSize} players.");
            }
        }
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private static double GetInitialScore(ScoreSystem scoreSystem) => scoreSystem switch
    {
        ScoreSystem.Elo => 1000d,
        ScoreSystem.TrueSkill => (25d - (3d * 8.333d)) * 100d,
        ScoreSystem.Lives => DefaultLives,
        ScoreSystem.WinCount => 0d,
        _ => 0d
    };
}
