using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name, int TeamSize, int PointsToWin, ScoreSystem ScoreSystem,
    int? MaxPlayerCount, bool IsPublic, bool CreateAndPlan = false) : IRequest<Guid>;

public record SetSeedTournamentCommand(Guid TournamentId, Guid SeedTournamentId) : IRequest;

public class TournamentCommandHandlers(IdaslettenDbContext db,
    IMediator mediator) : IRequestHandler<CreateTournamentCommand, Guid>,
    IRequestHandler<SetSeedTournamentCommand>
{
    private readonly IdaslettenDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<Guid> Handle(CreateTournamentCommand req, CancellationToken ct)
    {
        var t = new Tournament
        {
            Name = req.Name, TeamSize = req.TeamSize, PointsToWin = req.PointsToWin,
            ScoreSystem = req.ScoreSystem, MaxPlayerCount = req.MaxPlayerCount, IsPublic = req.IsPublic
        };
        _db.Tournaments.Add(t);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new TournamentCreated(t.Id, t.Name), ct);
        return t.Id;
    }

    public async Task Handle(SetSeedTournamentCommand req, CancellationToken ct)
    {
        var t = await _db.Tournaments.FindAsync(req.TournamentId) ?? throw new InvalidOperationException("NotFound");
        if (t.ParentTournamentId.HasValue)
            throw new InvalidOperationException("Tournament with a parent cannot be seeded.");
        t.SeedTournamentId = req.SeedTournamentId;
        await _db.SaveChangesAsync(ct);
    }
}

public record TournamentCreated(Guid TournamentId, string Name) : INotification;