using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    int TeamSize = 2,
    int PointsToWin = 5,
    ScoreSystem ScoreSystem = ScoreSystem.Elo,
    int? MaxPlayerCount = null,
    bool IsPublic = true,
    Guid? SeedTournamentId = null) : IRequest<Guid>;

public class TournamentCreated : INotification
{
    public Guid TournamentId { get; set; }
}

public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IPublisher _publisher;

    public CreateTournamentHandler(Shared.Data.ApplicationDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

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
            SeedTournamentId = request.SeedTournamentId,
            RoundNumber = 1
        };

        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TournamentCreated { TournamentId = tournament.Id }, cancellationToken);
        return tournament.Id;
    }
}
