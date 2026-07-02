using Idasletten.Shared;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    int TeamSize = 2,
    int PointsToWin = 5,
    ScoreSystem ScoreSystem = ScoreSystem.Elo,
    int? MaxPlayerCount = null,
    bool IsPublic = true) : IRequest<Tournament>;

public record TournamentCreated(Guid TournamentId, string Name) : INotification;

public class CreateTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<CreateTournamentCommand, Tournament>
{
    public async Task<Tournament> Handle(CreateTournamentCommand request, CancellationToken ct)
    {
        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            TeamSize = Math.Max(1, request.TeamSize),
            PointsToWin = Math.Max(1, request.PointsToWin),
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new TournamentCreated(tournament.Id, tournament.Name), ct);
        return tournament;
    }
}
