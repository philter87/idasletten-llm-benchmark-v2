using Idasletten.Features.Tournaments.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record TournamentSummaryDto(
    Guid Id,
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsArchived,
    bool IsPublic,
    int PlayerCount,
    int MatchCount,
    Guid? ParentTournamentId,
    int? RoundNumber);

public record GetTournamentsQuery(bool IncludeArchived, bool IncludePrivate, bool IncludeChildTournaments) : IRequest<IReadOnlyList<TournamentSummaryDto>>;

public sealed class GetTournamentsHandler(AppDbContext db, IMediator mediator) : IRequestHandler<GetTournamentsQuery, IReadOnlyList<TournamentSummaryDto>>
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<TournamentSummaryDto>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tournaments.AsNoTracking().AsQueryable();

        if (!request.IncludeArchived)
        {
            query = query.Where(tournament => !tournament.IsArchived);
        }

        if (!request.IncludePrivate)
        {
            query = query.Where(tournament => tournament.IsPublic);
        }

        if (!request.IncludeChildTournaments)
        {
            query = query.Where(tournament => tournament.ParentTournamentId == null);
        }

        return await query
            .OrderBy(tournament => tournament.Name)
            .Select(tournament => new TournamentSummaryDto(
                tournament.Id,
                tournament.Name,
                tournament.TeamSize,
                tournament.PointsToWin,
                tournament.ScoreSystem,
                tournament.MaxPlayerCount,
                tournament.IsArchived,
                tournament.IsPublic,
                tournament.Players.Count,
                tournament.Matches.Count,
                tournament.ParentTournamentId,
                tournament.RoundNumber))
            .ToListAsync(cancellationToken);
    }
}
