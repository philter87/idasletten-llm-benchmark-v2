using Idasletten.Features.Matches.Entities;
using Idasletten.Features.Matches.Events;
using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Users.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record TeamInput(IReadOnlyList<string> PlayerInitials, int Goals);

public record RecordMatchResultCommand(
    Guid TournamentId,
    IReadOnlyList<TeamInput> Teams,
    Guid? ExistingMatchId = null) : IRequest<Guid>;

public sealed class RecordMatchResultHandler(AppDbContext db, IMediator mediator) : IRequestHandler<RecordMatchResultCommand, Guid>
{
    private readonly AppDbContext _db = db;
    private readonly IMediator _mediator = mediator;
    private const int DefaultLives = 3;

    public async Task<Guid> Handle(RecordMatchResultCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(value => value.Id == request.TournamentId, cancellationToken);

        if (tournament is null)
        {
            throw new InvalidOperationException($"Tournament '{request.TournamentId}' was not found.");
        }

        ValidateTeams(request.Teams, tournament.TeamSize);

        var preparedTeams = request.Teams
            .Select(team => new PreparedTeamInput(
                team.PlayerInitials.Select(initial => NormalizeRequired(initial, nameof(team.PlayerInitials))).ToList(),
                team.Goals))
            .ToList();

        var duplicateNames = preparedTeams
            .SelectMany(team => team.PlayerInitials)
            .GroupBy(initial => initial, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            throw new InvalidOperationException($"A player may only appear once per match. Duplicates: {string.Join(", ", duplicateNames)}.");
        }

        var usernameKeys = preparedTeams
            .SelectMany(team => team.PlayerInitials)
            .Select(initial => initial.ToLowerInvariant())
            .Distinct()
            .ToList();

        var users = await _db.Users
            .Where(user => usernameKeys.Contains(user.Username.ToLower()))
            .ToListAsync(cancellationToken);

        var usersByKey = users.ToDictionary(user => user.Username.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

        foreach (var username in preparedTeams.SelectMany(team => team.PlayerInitials))
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
            .Include(player => player.User)
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
                User = user,
                Score = GetInitialScore(tournament.ScoreSystem),
                Lives = DefaultLives
            };

            _db.TournamentPlayers.Add(tournamentPlayer);
            playersByUserId[user.Id] = tournamentPlayer;
        }

        var currentMaxTeamNumber = await _db.TournamentTeams
            .Where(team => team.TournamentId == tournament.Id)
            .Select(team => (int?)team.Number)
            .MaxAsync(cancellationToken) ?? 0;

        TournamentMatch match;
        if (request.ExistingMatchId.HasValue)
        {
            match = await _db.TournamentMatches
                .Include(existingMatch => existingMatch.TeamResults)
                    .ThenInclude(result => result.Team)
                        .ThenInclude(team => team.TeamPlayers)
                .FirstOrDefaultAsync(existingMatch => existingMatch.Id == request.ExistingMatchId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Match '{request.ExistingMatchId}' was not found.");

            if (match.TournamentId != tournament.Id)
            {
                throw new InvalidOperationException("The existing match does not belong to the supplied tournament.");
            }

            var teamsToRemove = match.TeamResults
                .Select(result => result.Team)
                .Distinct()
                .ToList();

            var teamPlayersToRemove = teamsToRemove
                .SelectMany(team => team.TeamPlayers)
                .ToList();

            _db.TournamentTeamMatchResults.RemoveRange(match.TeamResults);
            _db.TournamentTeamPlayers.RemoveRange(teamPlayersToRemove);
            _db.TournamentTeams.RemoveRange(teamsToRemove);
            match.TeamResults.Clear();
        }
        else
        {
            var nextOrder = (await _db.TournamentMatches
                .Where(existingMatch => existingMatch.TournamentId == tournament.Id)
                .Select(existingMatch => (int?)existingMatch.Order)
                .MaxAsync(cancellationToken) ?? 0) + 1;

            match = new TournamentMatch
            {
                TournamentId = tournament.Id,
                Order = nextOrder
            };

            _db.TournamentMatches.Add(match);
        }

        var losingGoals = preparedTeams.Select(team => team.Goals).OrderByDescending(goal => goal).Skip(1).First();
        var winningGoals = preparedTeams.Max(team => team.Goals);

        if (winningGoals == losingGoals)
        {
            throw new InvalidOperationException("A recorded match result cannot end in a tie.");
        }

        var currentScores = playersByUserId.Values.ToDictionary(player => player.Id, player => player.Score);
        var matchResults = new List<PlayerMatchResult>();

        foreach (var team in preparedTeams)
        {
            var teamNumber = ++currentMaxTeamNumber;
            var teamEntity = new TournamentTeam
            {
                TournamentId = tournament.Id,
                Number = teamNumber,
                Name = $"Team {teamNumber}"
            };

            _db.TournamentTeams.Add(teamEntity);

            var goalsLost = preparedTeams.Single(otherTeam => !ReferenceEquals(otherTeam, team)).Goals;
            var won = team.Goals > goalsLost;

            foreach (var username in team.PlayerInitials)
            {
                var user = usersByKey[username.ToLowerInvariant()];
                var player = playersByUserId[user.Id];

                _db.TournamentTeamPlayers.Add(new TournamentTeamPlayer
                {
                    Team = teamEntity,
                    Player = player
                });

                player.MatchCount += 1;
                player.PointsWon += team.Goals;
                player.PointsLost += goalsLost;

                if (won)
                {
                    player.WinCount += 1;
                }
                else
                {
                    player.LoseCount += 1;
                    if (tournament.ScoreSystem == ScoreSystem.Lives)
                    {
                        player.Lives = Math.Max(0, player.Lives - 1);
                    }
                }

                matchResults.Add(new PlayerMatchResult(player.Id, team.Goals, goalsLost, won));
            }

            _db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
            {
                Match = match,
                TournamentId = tournament.Id,
                Team = teamEntity,
                GoalsWon = team.Goals,
                GoalsLost = goalsLost
            });
        }

        var scoreCalculator = ScoreCalculatorFactory.GetCalculator(tournament.ScoreSystem);
        var scoreUpdates = scoreCalculator.CalculateScores(matchResults, currentScores)
            .ToDictionary(update => update.PlayerId);

        foreach (var player in playersByUserId.Values)
        {
            if (scoreUpdates.TryGetValue(player.Id, out var update))
            {
                player.Score = update.NewScore;
                player.ScoreDiff = update.ScoreDiff;
            }
            else
            {
                player.ScoreDiff = 0;
            }

            if (tournament.ScoreSystem == ScoreSystem.Lives)
            {
                player.Score = player.Lives;
            }
        }

        match.State = MatchState.Done;
        match.PlayedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _mediator.Publish(new MatchResultRecorded(match.Id, tournament.Id), cancellationToken);

        return match.Id;
    }

    private static void ValidateTeams(IReadOnlyList<TeamInput> teams, int expectedTeamSize)
    {
        if (teams.Count != 2)
        {
            throw new InvalidOperationException("A match result must contain exactly two teams.");
        }

        foreach (var team in teams)
        {
            if (team.PlayerInitials is null || team.PlayerInitials.Count != expectedTeamSize)
            {
                throw new InvalidOperationException($"Each team must contain exactly {expectedTeamSize} players.");
            }

            if (team.Goals < 0)
            {
                throw new InvalidOperationException("Goals cannot be negative.");
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

    private sealed record PreparedTeamInput(IReadOnlyList<string> PlayerInitials, int Goals);
}
