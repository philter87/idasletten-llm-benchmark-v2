using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreateTournamentHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreateTournamentCommand command, CancellationToken ct)
    {
        int? roundNumber = null;
        if (command.ParentTournamentId.HasValue)
        {
            var parent = await _db.Tournaments.FindAsync([command.ParentTournamentId.Value], ct);
            if (parent != null)
            {
                roundNumber = (parent.RoundNumber ?? 0) + 1;
            }
        }

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            TeamSize = command.TeamSize,
            PointsToWin = command.PointsToWin,
            ScoreSystem = command.ScoreSystem,
            MaxPlayerCount = command.MaxPlayerCount,
            IsPublic = command.IsPublic,
            IsArchived = false,
            SeedTournamentId = command.SeedTournamentId,
            ParentTournamentId = command.ParentTournamentId,
            RoundNumber = roundNumber ?? 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new TournamentCreated(tournament.Id), ct);
        return tournament.Id;
    }
}
