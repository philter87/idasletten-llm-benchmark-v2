using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class TournamentDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public int PointsToWin { get; set; }
    public ScoreSystem ScoreSystem { get; set; }
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int RoundNumber { get; set; }
    public List<Features.Matches.Queries.MatchListItemDto> NextPlannedMatches { get; set; } = [];
    public List<Features.Matches.Queries.MatchListItemDto> RecentDoneMatches { get; set; } = [];
}

public record GetTournamentDetailQuery(Guid TournamentId) : IRequest<TournamentDetailDto?>;

public class GetTournamentDetailHandler : IRequestHandler<GetTournamentDetailQuery, TournamentDetailDto?>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public GetTournamentDetailHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TournamentDetailDto?> Handle(GetTournamentDetailQuery request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (tournament == null) return null;

        var planned = await _db.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == request.TournamentId && m.State == Features.Matches.MatchState.Planned)
            .OrderBy(m => m.Order)
            .Take(5)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Members)
                    .ThenInclude(mp => mp.User)
            .ToListAsync(cancellationToken);

        var done = await _db.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == request.TournamentId && m.State == Features.Matches.MatchState.Done)
            .OrderByDescending(m => m.CompletedAt)
            .Take(5)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Members)
                    .ThenInclude(mp => mp.User)
            .ToListAsync(cancellationToken);

        static Features.Matches.Queries.MatchListItemDto Map(Features.Matches.TournamentMatch m) => new()
        {
            Id = m.Id,
            Order = m.Order,
            State = m.State,
            CompletedAt = m.CompletedAt,
            Teams = m.Teams.OrderBy(t => t.Number).Select(t => new Features.Matches.Queries.TeamListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                Number = t.Number,
                GoalsWon = t.GoalsWon,
                GoalsLost = t.GoalsLost,
                Members = t.Members.Select(mp => mp.User.Username).ToList()
            }).ToList()
        };

        return new TournamentDetailDto
        {
            Id = tournament.Id,
            Name = tournament.Name,
            TeamSize = tournament.TeamSize,
            PointsToWin = tournament.PointsToWin,
            ScoreSystem = tournament.ScoreSystem,
            MaxPlayerCount = tournament.MaxPlayerCount,
            IsArchived = tournament.IsArchived,
            IsPublic = tournament.IsPublic,
            SeedTournamentId = tournament.SeedTournamentId,
            ParentTournamentId = tournament.ParentTournamentId,
            RoundNumber = tournament.RoundNumber,
            NextPlannedMatches = planned.Select(Map).ToList(),
            RecentDoneMatches = done.Select(Map).ToList()
        };
    }
}
