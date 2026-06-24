using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using Idasletten.Shared.Events;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsPublic,
    Guid? SeedTournamentId,
    Guid? ParentTournamentId
) : IRequest<Guid>;

public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public CreateTournamentHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = new Tournament
        {
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            SeedTournamentId = request.SeedTournamentId,
            ParentTournamentId = request.ParentTournamentId
        };

        if (request.ParentTournamentId.HasValue)
        {
            var parent = await _db.Tournaments.FindAsync(new object[] { request.ParentTournamentId.Value }, cancellationToken);
            tournament.RoundNumber = (parent?.RoundNumber ?? 1) + 1;
        }
        else
        {
            tournament.RoundNumber = 1;
        }

        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new TournamentCreated(tournament.Id, tournament.Name), cancellationToken);

        return tournament.Id;
    }
}
