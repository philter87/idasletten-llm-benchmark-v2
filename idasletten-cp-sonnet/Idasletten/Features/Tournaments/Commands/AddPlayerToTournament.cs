using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Tournaments.Events;
using Idasletten.Features.Users.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public record AddPlayerToTournamentCommand(Guid TournamentId, string Username, string? Name) : IRequest<Guid>;

public sealed class AddPlayerToTournamentHandler(AppDbContext db, IMediator mediator) : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    private readonly AppDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<Guid> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var username = NormalizeRequired(request.Username, nameof(request.Username));
        var usernameKey = username.ToLowerInvariant();
        var displayName = string.IsNullOrWhiteSpace(request.Name) ? username : request.Name.Trim();

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament is null)
        {
            throw new InvalidOperationException($"Tournament '{request.TournamentId}' was not found.");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(existingUser => existingUser.Username.ToLower() == usernameKey, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Username = username,
                Name = displayName
            };

            _db.Users.Add(user);
        }

        var existingPlayer = await _db.TournamentPlayers
            .FirstOrDefaultAsync(player => player.TournamentId == tournament.Id && player.UserId == user.Id, cancellationToken);

        if (existingPlayer is not null)
        {
            return existingPlayer.Id;
        }

        if (tournament.MaxPlayerCount.HasValue)
        {
            var currentPlayerCount = await _db.TournamentPlayers
                .CountAsync(player => player.TournamentId == tournament.Id, cancellationToken);

            if (currentPlayerCount >= tournament.MaxPlayerCount.Value)
            {
                throw new InvalidOperationException("The tournament has reached its maximum player count.");
            }
        }

        var tournamentPlayer = new TournamentPlayer
        {
            TournamentId = tournament.Id,
            UserId = user.Id,
            Score = GetInitialScore(tournament.ScoreSystem),
            Lives = DefaultLives
        };

        _db.TournamentPlayers.Add(tournamentPlayer);
        await _db.SaveChangesAsync(cancellationToken);
        await _mediator.Publish(new PlayerAdded(tournament.Id, user.Id), cancellationToken);

        return tournamentPlayer.Id;
    }

    private const int DefaultLives = 3;

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
