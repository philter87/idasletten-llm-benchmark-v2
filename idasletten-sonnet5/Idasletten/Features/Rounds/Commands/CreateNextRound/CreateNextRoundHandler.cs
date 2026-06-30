using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Rounds.Commands.CreateNextRound;

public class CreateNextRoundHandler(IdaslettenDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<CreateNextRoundCommand, Guid>
{
    public async Task<Guid> Handle(CreateNextRoundCommand request, CancellationToken cancellationToken)
    {
        var parent = await db.Tournaments.FirstAsync(t => t.Id == request.ParentTournamentId, cancellationToken);
        var roundNumber = (parent.RoundNumber ?? 0) + 1;

        var newTournamentId = await sender.Send(new CreateTournamentCommand(
            request.Name,
            parent.TeamSize,
            parent.PointsToWin,
            parent.ScoreSystem,
            parent.MaxPlayerCount,
            parent.IsPublic,
            ParentTournamentId: parent.Id,
            RoundNumber: roundNumber), cancellationToken);

        var parentPlayers = await db.TournamentPlayers
            .Where(p => p.TournamentId == parent.Id)
            .OrderByDescending(p => p.Score)
            .ToListAsync(cancellationToken);

        if (request.TopN is { } topN)
        {
            parentPlayers = parentPlayers.Take(topN).ToList();
        }

        var calculator = ScoreCalculatorFactory.Create(parent.ScoreSystem);
        foreach (var parentPlayer in parentPlayers)
        {
            var newPlayer = new Features.TournamentPlayers.TournamentPlayer
            {
                Id = Guid.NewGuid(),
                TournamentId = newTournamentId,
                UserId = parentPlayer.UserId
            };
            calculator.ResetPlayer(newPlayer);
            db.TournamentPlayers.Add(newPlayer);
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new RoundCreated(newTournamentId, parent.Id, roundNumber), cancellationToken);

        return newTournamentId;
    }
}
