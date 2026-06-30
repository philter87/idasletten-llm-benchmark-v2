using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.CreatePlannedMatch;

public class CreatePlannedMatchHandler(IdaslettenDbContext db) : IRequestHandler<CreatePlannedMatchCommand, Guid>
{
    public async Task<Guid> Handle(CreatePlannedMatchCommand request, CancellationToken cancellationToken)
    {
        var nextOrder = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .Select(m => (int?)m.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var match = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            Order = nextOrder + 1,
            State = MatchState.Planned
        };

        db.TournamentMatches.Add(match);
        await db.SaveChangesAsync(cancellationToken);

        return match.Id;
    }
}
