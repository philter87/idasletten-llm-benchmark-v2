using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerToTournamentCommand(
    string Username,
    string? Name,
    Guid TournamentId
) : IRequest<TournamentPlayer>;

public class AddPlayerToTournamentHandler : IRequestHandler<AddPlayerToTournamentCommand, TournamentPlayer>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public AddPlayerToTournamentHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<TournamentPlayer> Handle(AddPlayerToTournamentCommand request, CancellationToken ct)
    {
        var username = request.Username.ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user == null)
        {
            user = new User
            {
                Username = username,
                Name = string.IsNullOrWhiteSpace(request.Name) ? username : request.Name
            };
            _db.Users.Add(user);
        }

        var tournament = await _db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var existingPlayer = await _db.TournamentPlayers
            .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == request.TournamentId, ct);

        if (existingPlayer != null)
            return existingPlayer;

        var player = new TournamentPlayer
        {
            UserId = user.Id,
            TournamentId = request.TournamentId,
            Score = tournament.ScoreSystem == ScoreSystem.Elo ? 1000 : 0,
            Lives = 3
        };

        _db.TournamentPlayers.Add(player);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new PlayerAddedToTournament(player.Id, request.TournamentId), ct);

        return player;
    }
}

public record PlayerAddedToTournament(Guid PlayerId, Guid TournamentId) : INotification;
