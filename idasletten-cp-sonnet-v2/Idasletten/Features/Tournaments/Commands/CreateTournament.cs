using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
) : IRequest<Tournament>;

public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Tournament>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreateTournamentHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Tournament> Handle(CreateTournamentCommand request, CancellationToken ct)
    {
        int? roundNumber = null;
        if (request.ParentTournamentId.HasValue)
        {
            var siblings = await _db.Tournaments
                .Where(t => t.ParentTournamentId == request.ParentTournamentId)
                .CountAsync(ct);
            roundNumber = siblings + 2; // parent is round 1, first child is round 2
        }

        var tournament = new Tournament
        {
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            SeedTournamentId = request.SeedTournamentId,
            ParentTournamentId = request.ParentTournamentId,
            RoundNumber = roundNumber
        };

        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new TournamentCreated(tournament.Id), ct);

        return tournament;
    }
}

public record TournamentCreated(Guid TournamentId) : INotification;
