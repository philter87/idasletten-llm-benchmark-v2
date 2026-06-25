using Idasletten.Data;
using Idasletten.Features.Tournaments.Events;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            ParentTournamentId = request.ParentTournamentId
        };

        if (request.ParentTournamentId.HasValue)
        {
            var parent = await _db.Tournaments.FindAsync(new object[] { request.ParentTournamentId.Value }, cancellationToken);
            if (parent != null)
            {
                tournament.RoundNumber = parent.RoundNumber + 1;
            }
        }

        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TournamentCreated(tournament.Id), cancellationToken);

        return tournament.Id;
    }
}
