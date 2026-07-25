using Idasletten.Features.Matches;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

/// <summary>
/// Lists tournaments for the front page and the "all tournaments" page. Child tournaments (later
/// rounds) are hidden unless they are explicitly asked for.
/// </summary>
public record GetTournaments(
    bool OnlyPublic = false,
    bool IncludeArchived = true,
    bool IncludeRounds = false) : IRequest<IReadOnlyList<TournamentSummary>>;

public record TournamentSummary(
    Guid Id,
    string Name,
    ScoreSystem ScoreSystem,
    int TeamSize,
    int PointsToWin,
    bool IsPublic,
    bool IsArchived,
    int? RoundNumber,
    Guid? ParentTournamentId,
    int RoundCount,
    int PlayerCount,
    int PlayedMatchCount,
    int PlannedMatchCount,
    DateTime CreatedUtc,
    string? LeaderName,
    double? LeaderScore);

public class GetTournamentsHandler(AppDbContext db)
    : IRequestHandler<GetTournaments, IReadOnlyList<TournamentSummary>>
{
    public async Task<IReadOnlyList<TournamentSummary>> Handle(
        GetTournaments request, CancellationToken cancellationToken)
    {
        var query = db.Tournaments.AsNoTracking().AsQueryable();

        if (request.OnlyPublic)
        {
            query = query.Where(t => t.IsPublic);
        }

        if (!request.IncludeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        if (!request.IncludeRounds)
        {
            query = query.Where(t => t.ParentTournamentId == null);
        }

        var summaries = await query
            .OrderByDescending(t => t.CreatedUtc)
            .Select(t => new TournamentSummary(
                t.Id,
                t.Name,
                t.ScoreSystem,
                t.TeamSize,
                t.PointsToWin,
                t.IsPublic,
                t.IsArchived,
                t.RoundNumber,
                t.ParentTournamentId,
                t.Rounds.Count,
                t.Players.Count,
                t.Matches.Count(m => m.State == MatchState.Done),
                t.Matches.Count(m => m.State == MatchState.Planned),
                t.CreatedUtc,
                t.Players
                    .OrderByDescending(p => p.Score)
                    .Select(p => p.User.Name ?? p.User.UserName)
                    .FirstOrDefault(),
                t.Players
                    .OrderByDescending(p => p.Score)
                    .Select(p => (double?)p.Score)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return summaries;
    }
}
