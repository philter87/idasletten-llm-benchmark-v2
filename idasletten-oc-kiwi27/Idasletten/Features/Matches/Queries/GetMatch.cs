using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public class MatchDetailDto : MatchListItemDto
{
    public Guid TournamentId { get; set; }
    public int PointsToWin { get; set; }
}

public record GetMatchQuery(Guid MatchId) : IRequest<MatchDetailDto?>;

public class GetMatchHandler : IRequestHandler<GetMatchQuery, MatchDetailDto?>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public GetMatchHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MatchDetailDto?> Handle(GetMatchQuery request, CancellationToken cancellationToken)
    {
        var match = await _db.TournamentMatches
            .AsNoTracking()
            .Include(m => m.Tournament)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Members)
                    .ThenInclude(mp => mp.User)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match == null) return null;

        return new MatchDetailDto
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            Order = match.Order,
            State = match.State,
            CompletedAt = match.CompletedAt,
            PointsToWin = match.Tournament.PointsToWin,
            Teams = match.Teams.OrderBy(t => t.Number).Select(t => new TeamListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                Number = t.Number,
                GoalsWon = t.GoalsWon,
                GoalsLost = t.GoalsLost,
                Members = t.Members.Select(mp => mp.User.Username).ToList()
            }).ToList()
        };
    }
}
