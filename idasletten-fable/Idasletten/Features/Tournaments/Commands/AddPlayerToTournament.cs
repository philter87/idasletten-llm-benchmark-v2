using Idasletten.Features.Scoring;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

/// <summary>
/// Adds a player to a tournament by initials. Creates the User if the initials are
/// unknown. Idempotent: returns the existing TournamentPlayer if already joined.
/// </summary>
public record AddPlayerToTournamentCommand(Guid TournamentId, string Initials, string? Name = null)
    : IRequest<TournamentPlayer>;

public record PlayerAddedToTournament(Guid TournamentId, Guid UserId, Guid TournamentPlayerId) : INotification;

public class AddPlayerToTournamentHandler(AppDbContext db, IMediator mediator, IPublisher publisher)
    : IRequestHandler<AddPlayerToTournamentCommand, TournamentPlayer>
{
    public async Task<TournamentPlayer> Handle(AddPlayerToTournamentCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        var user = await mediator.Send(new CreateUserCommand(request.Initials, request.Name), ct);

        var existing = await db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == tournament.Id && p.UserId == user.Id, ct);
        if (existing is not null)
            return existing;

        var playerCount = await db.TournamentPlayers.CountAsync(p => p.TournamentId == tournament.Id, ct);
        if (tournament.MaxPlayerCount is int max && playerCount >= max)
            throw new InvalidOperationException($"Tournament '{tournament.Name}' is full ({max} players).");

        var player = new TournamentPlayer
        {
            TournamentId = tournament.Id,
            UserId = user.Id,
            User = user
        };
        ScoringEngine.ResetPlayer(player, tournament.ScoreSystem);
        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new PlayerAddedToTournament(tournament.Id, user.Id, player.Id), ct);
        return player;
    }
}
