using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<Guid>;

public class AddPlayerHandler : IRequestHandler<AddPlayerCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public AddPlayerHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(AddPlayerCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found.");

        if (tournament.MaxPlayerCount.HasValue && tournament.Players.Count >= tournament.MaxPlayerCount.Value)
            throw new InvalidOperationException("Tournament is full.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Initials == request.Initials, cancellationToken);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Initials,
                Initials = request.Initials,
                Name = request.Name ?? request.Initials
            };
            _db.Users.Add(user);
            await _publisher.Publish(new UserCreated(user.Id, user.Initials, user.Name), cancellationToken);
        }

        var existingPlayer = await _db.TournamentPlayers
            .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == request.TournamentId, cancellationToken);

        if (existingPlayer != null)
            return user.Id;

        var player = new TournamentPlayer
        {
            UserId = user.Id,
            TournamentId = request.TournamentId,
            Score = 1000,
            Lives = 3
        };
        _db.TournamentPlayers.Add(player);
        await _db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
