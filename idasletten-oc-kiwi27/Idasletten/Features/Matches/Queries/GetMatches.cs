using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchesQuery(Guid TournamentId) : IRequest<MatchesDto>;

public class MatchesDto
{
    public List<MatchListItemDto> Planned { get; set; } = [];
    public List<MatchListItemDto> Done { get; set; } = [];
}

public class GetMatchesHandler : IRequestHandler<GetMatchesQuery, MatchesDto>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public GetMatchesHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MatchesDto> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await _db.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == request.TournamentId)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Members)
                    .ThenInclude(mp => mp.User)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        static MatchListItemDto Map(TournamentMatch m) => new()
        {
            Id = m.Id,
            Order = m.Order,
            State = m.State,
            CompletedAt = m.CompletedAt,
            Teams = m.Teams.OrderBy(t => t.Number).Select(t => new TeamListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                Number = t.Number,
                GoalsWon = t.GoalsWon,
                GoalsLost = t.GoalsLost,
                Members = t.Members.Select(mp => mp.User.Username).ToList()
            }).ToList()
        };

        return new MatchesDto
        {
            Planned = matches.Where(m => m.State == MatchState.Planned).Select(Map).ToList(),
            Done = matches.Where(m => m.State == MatchState.Done).Select(Map).ToList()
        };
    }
}
