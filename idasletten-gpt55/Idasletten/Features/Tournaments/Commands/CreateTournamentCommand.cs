using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(string Name, int TeamSize, int PointsToWin, ScoreSystem ScoreSystem, int? MaxPlayerCount, bool IsPublic, Guid? SeedTournamentId = null, Guid? ParentTournamentId = null) : IRequest<Guid>;

public class CreateTournamentHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<CreateTournamentCommand, Guid>
{
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentTournamentId.HasValue && request.SeedTournamentId.HasValue) throw new InvalidOperationException("A child tournament cannot also be seeded.");
        var roundNumber = 1;
        if (request.ParentTournamentId.HasValue)
        {
            var parentRound = await db.Tournaments.Where(tournament => tournament.Id == request.ParentTournamentId.Value).Select(tournament => tournament.RoundNumber ?? 1).SingleAsync(cancellationToken);
            roundNumber = parentRound + 1;
        }

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            TeamSize = Math.Max(1, request.TeamSize),
            PointsToWin = Math.Max(1, request.PointsToWin),
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            SeedTournamentId = request.SeedTournamentId,
            ParentTournamentId = request.ParentTournamentId,
            RoundNumber = request.ParentTournamentId.HasValue ? roundNumber : null
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new TournamentCreated(tournament.Id), cancellationToken);
        return tournament.Id;
    }
}
