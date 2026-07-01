using Idasletten.Data;
using Idasletten.Features.Players.Commands.AddPlayerToTournament;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands.AddExistingUserToTournament;

public class AddExistingUserToTournamentHandler(IdaslettenDbContext db, IPublisher publisher)
    : IRequestHandler<AddExistingUserToTournamentCommand, Guid>
{
    public async Task<Guid> Handle(AddExistingUserToTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstAsync(t => t.Id == request.TournamentId, cancellationToken);

        var existing = tournament.Players.FirstOrDefault(p => p.UserId == request.UserId);
        if (existing is not null)
        {
            return existing.Id;
        }

        if (tournament.MaxPlayerCount is { } max && tournament.Players.Count >= max)
        {
            throw new InvalidOperationException($"Tournament already has the maximum of {max} players.");
        }

        var strategy = ScoreSystemStrategyFactory.Create(tournament.ScoreSystem);
        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            UserId = request.UserId,
            Score = strategy.InitialScore,
            Lives = 3,
        };

        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new PlayerAddedToTournament(tournament.Id, player.Id, request.UserId), cancellationToken);

        return player.Id;
    }
}
