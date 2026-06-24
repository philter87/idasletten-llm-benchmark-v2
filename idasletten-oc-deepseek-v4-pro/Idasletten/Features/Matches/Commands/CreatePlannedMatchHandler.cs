using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class CreatePlannedMatchHandler : IRequestHandler<CreatePlannedMatchCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreatePlannedMatchHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreatePlannedMatchCommand command, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync([command.TournamentId], ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var match = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = command.TournamentId,
            Order = await _db.TournamentMatches.CountAsync(m => m.TournamentId == command.TournamentId, ct) + 1,
            State = MatchState.Planned,
            CreatedAt = DateTime.UtcNow
        };

        _db.TournamentMatches.Add(match);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new PlannedMatchCreated(match.Id, match.TournamentId), ct);
        return match.Id;
    }
}
