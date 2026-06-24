using Idasletten.Shared.Data;
using Idasletten.Shared.Enums;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record UpdateTournamentCommand(
    Guid Id,
    string? Name,
    int? TeamSize,
    int? PointsToWin,
    ScoreSystem? ScoreSystem,
    int? MaxPlayerCount,
    bool? IsPublic,
    bool? IsArchived
) : IRequest;

public class UpdateTournamentHandler : IRequestHandler<UpdateTournamentCommand>
{
    private readonly AppDbContext _db;

    public UpdateTournamentHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UpdateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { request.Id }, cancellationToken);
        if (tournament == null) return;

        if (request.Name != null) tournament.Name = request.Name;
        if (request.TeamSize.HasValue) tournament.TeamSize = request.TeamSize.Value;
        if (request.PointsToWin.HasValue) tournament.PointsToWin = request.PointsToWin.Value;
        if (request.ScoreSystem.HasValue) tournament.ScoreSystem = request.ScoreSystem.Value;
        if (request.MaxPlayerCount.HasValue) tournament.MaxPlayerCount = request.MaxPlayerCount;
        if (request.IsPublic.HasValue) tournament.IsPublic = request.IsPublic.Value;
        if (request.IsArchived.HasValue) tournament.IsArchived = request.IsArchived.Value;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
