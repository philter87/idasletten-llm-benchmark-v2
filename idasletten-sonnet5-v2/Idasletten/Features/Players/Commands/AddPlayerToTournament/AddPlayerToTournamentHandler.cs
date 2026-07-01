using Idasletten.Data;
using Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands.AddPlayerToTournament;

public class AddPlayerToTournamentHandler(IdaslettenDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    public async Task<Guid> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstAsync(t => t.Id == request.TournamentId, cancellationToken);

        var user = await sender.Send(new GetOrCreateUserByUsernameCommand(request.Username, request.Name), cancellationToken);

        var existing = tournament.Players.FirstOrDefault(p => p.UserId == user.Id);
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
            UserId = user.Id,
            Score = strategy.InitialScore,
            Lives = 3,
        };

        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new PlayerAddedToTournament(tournament.Id, player.Id, user.Id), cancellationToken);

        return player.Id;
    }
}
