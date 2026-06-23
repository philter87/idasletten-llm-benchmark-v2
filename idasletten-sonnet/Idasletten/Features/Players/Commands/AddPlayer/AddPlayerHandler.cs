using Idasletten.Features.Players.Events;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands.AddPlayer;

public class AddPlayerHandler(AppDbContext db, IPublisher publisher) : IRequestHandler<AddPlayerCommand, Guid>
{
    public async Task<Guid> Handle(AddPlayerCommand request, CancellationToken cancellationToken)
    {
        var initials = request.Initials.ToUpper();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == initials, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = initials,
                Name = request.Name ?? initials,
            };
            db.Users.Add(user);
        }

        var existing = await db.TournamentPlayers
            .FirstOrDefaultAsync(tp => tp.TournamentId == request.TournamentId && tp.UserId == user.Id, cancellationToken);

        if (existing is not null) return existing.Id;

        var tournament = await db.Tournaments.FindAsync([request.TournamentId], cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TournamentId = request.TournamentId,
            Score = tournament.ScoreSystem == Idasletten.Shared.Enums.ScoreSystem.Elo
                || tournament.ScoreSystem == Idasletten.Shared.Enums.ScoreSystem.TrueSkill ? 1000 : 0,
            Lives = 3,
        };

        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new PlayerAdded(request.TournamentId, user.Id), cancellationToken);

        return player.Id;
    }
}
