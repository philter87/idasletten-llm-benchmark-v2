using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Matches.Events;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.CancelMatch;

public sealed record CancelMatchCommand(Guid MatchId) : IRequest<Unit>;

public sealed class CancelMatchCommandHandler : IRequestHandler<CancelMatchCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public CancelMatchCommandHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(CancelMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _db.TournamentMatches
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken)
            ?? throw new FeatureException("Match not found.");
        if (match.State != MatchState.Planned)
            throw new FeatureException("Only planned matches can be cancelled.");

        match.State = MatchState.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new MatchCancelled(match.Id, match.TournamentId), cancellationToken);
        return Unit.Value;
    }
}
