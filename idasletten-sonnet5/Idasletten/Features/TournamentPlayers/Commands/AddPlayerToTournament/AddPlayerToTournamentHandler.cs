using Idasletten.Features.Users.Commands.CreateUser;
using Idasletten.Shared.Data;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;

public class AddPlayerToTournamentHandler(IdaslettenDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    public async Task<Guid> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FirstAsync(t => t.Id == request.TournamentId, cancellationToken);
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();

        var userId = await db.Users
            .Where(u => u.NormalizedUserName == normalizedUsername)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            var username = request.Username.Trim();
            userId = await sender.Send(new CreateUserCommand(username, request.Name ?? username), cancellationToken);
        }

        var existingPlayer = await db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == request.TournamentId && p.UserId == userId, cancellationToken);
        if (existingPlayer is not null)
        {
            return existingPlayer.Id;
        }

        if (tournament.MaxPlayerCount is { } max)
        {
            var currentCount = await db.TournamentPlayers.CountAsync(p => p.TournamentId == request.TournamentId, cancellationToken);
            if (currentCount >= max)
            {
                throw new InvalidOperationException($"Tournament already has the maximum of {max} players.");
            }
        }

        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            UserId = userId.Value
        };
        ScoreCalculatorFactory.Create(tournament.ScoreSystem).ResetPlayer(player);

        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new PlayerAddedToTournament(player.Id, player.TournamentId, player.UserId), cancellationToken);

        return player.Id;
    }
}
