using Idasletten.Data;
using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<CreateTournamentCommand, Guid>
{
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TournamentCreated(tournament.Id), cancellationToken);

        return tournament.Id;
    }
}
