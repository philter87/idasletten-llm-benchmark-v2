using Idasletten.Features.Matches.Events;
using Idasletten.Features.Scoring;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record CancelMatch(Guid TournamentId, Guid MatchId) : IRequest<Unit>;

public class CancelMatchHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<CancelMatch, Unit>
{
    public async Task<Unit> Handle(CancelMatch request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches
            .FirstOrDefaultAsync(
                m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, cancellationToken);

        if (match is null)
        {
            return Unit.Value;
        }

        match.State = MatchState.Cancelled;
        await db.SaveChangesAsync(cancellationToken);

        // A cancelled match no longer counts, so the tournament has to be scored again.
        await TournamentScoring.RecalculateAsync(db, request.TournamentId, cancellationToken);

        await publisher.Publish(new MatchCancelled(request.TournamentId, match.Id), cancellationToken);

        return Unit.Value;
    }
}
