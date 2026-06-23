using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentHandler(AppDbContext db, IPublisher publisher) : IRequestHandler<CreateTournamentCommand, Guid>
{
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        int? roundNumber = null;

        if (request.ParentTournamentId.HasValue)
        {
            var parent = await db.Tournaments.FindAsync([request.ParentTournamentId.Value], cancellationToken);
            roundNumber = (parent?.RoundNumber ?? 1) + 1;
        }

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            ParentTournamentId = request.ParentTournamentId,
            SeedTournamentId = request.ParentTournamentId.HasValue ? null : request.SeedTournamentId,
            RoundNumber = roundNumber,
        };

        db.Tournaments.Add(tournament);

        if (request.ParentTournamentId.HasValue)
        {
            var parentPlayers = await db.TournamentPlayers
                .Where(tp => tp.TournamentId == request.ParentTournamentId.Value)
                .ToListAsync(cancellationToken);

            var newPlayers = parentPlayers.Select(pp => new TournamentPlayer
            {
                Id = Guid.NewGuid(),
                UserId = pp.UserId,
                TournamentId = tournament.Id,
                Score = 1000,
                Lives = 3,
            });

            db.TournamentPlayers.AddRange(newPlayers);
        }

        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new TournamentCreated(tournament.Id, tournament.Name), cancellationToken);

        return tournament.Id;
    }
}
