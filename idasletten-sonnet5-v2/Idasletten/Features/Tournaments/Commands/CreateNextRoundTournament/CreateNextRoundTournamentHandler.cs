using Idasletten.Data;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands.CreateNextRoundTournament;

public class CreateNextRoundTournamentHandler(IdaslettenDbContext db, IPublisher publisher)
    : IRequestHandler<CreateNextRoundTournamentCommand, Guid>
{
    public async Task<Guid> Handle(CreateNextRoundTournamentCommand request, CancellationToken cancellationToken)
    {
        var parent = await db.Tournaments
            .Include(t => t.Players)
            .FirstAsync(t => t.Id == request.ParentTournamentId, cancellationToken);

        var advancingPlayers = parent.Players
            .OrderByDescending(p => p.Score)
            .Take(request.TopPlayerCount ?? parent.Players.Count)
            .ToList();

        var round = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name ?? $"{parent.Name} - Round {(parent.RoundNumber ?? 1) + 1}",
            TeamSize = parent.TeamSize,
            PointsToWin = parent.PointsToWin,
            ScoreSystem = parent.ScoreSystem,
            MaxPlayerCount = parent.MaxPlayerCount,
            IsPublic = parent.IsPublic,
            ParentTournamentId = parent.Id,
            RoundNumber = (parent.RoundNumber ?? 1) + 1,
        };

        db.Tournaments.Add(round);

        var strategy = Shared.Scoring.ScoreSystemStrategyFactory.Create(round.ScoreSystem);
        foreach (var player in advancingPlayers)
        {
            db.TournamentPlayers.Add(new TournamentPlayer
            {
                Id = Guid.NewGuid(),
                TournamentId = round.Id,
                UserId = player.UserId,
                Score = strategy.InitialScore,
                Lives = 3,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TournamentCreated(round.Id), cancellationToken);

        return round.Id;
    }
}
