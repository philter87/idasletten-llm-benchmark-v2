using Idasletten.Shared.Data;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentHandler(IdaslettenDbContext db, IPublisher publisher)
    : IRequestHandler<CreateTournamentCommand, Guid>
{
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        // A tournament may be seeded only if it has no parent.
        var seedTournamentId = request.ParentTournamentId is null ? request.SeedTournamentId : null;

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            IsArchived = false,
            SeedTournamentId = seedTournamentId,
            ParentTournamentId = request.ParentTournamentId,
            RoundNumber = request.ParentTournamentId is null ? null : request.RoundNumber ?? 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TournamentCreated(tournament.Id, tournament.Name), cancellationToken);

        return tournament.Id;
    }
}
