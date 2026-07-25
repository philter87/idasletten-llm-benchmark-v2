using Idasletten.Features.Matches;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournament(Guid TournamentId) : IRequest<TournamentDetail?>;

public record TournamentDetail(
    Guid Id,
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsArchived,
    bool IsPublic,
    Guid? SeedTournamentId,
    string? SeedTournamentName,
    Guid? ParentTournamentId,
    string? ParentTournamentName,
    int? RoundNumber,
    DateTime CreatedUtc,
    int PlayerCount,
    int PlayedMatchCount,
    int PlannedMatchCount,
    IReadOnlyList<TournamentRound> Rounds)
{
    public bool CanBeSeeded => ParentTournamentId is null;
    public bool HasRoomForMorePlayers => MaxPlayerCount is null || PlayerCount < MaxPlayerCount;
}

public record TournamentRound(Guid Id, string Name, int RoundNumber, int PlayerCount);

public class GetTournamentHandler(AppDbContext db) : IRequestHandler<GetTournament, TournamentDetail?>
{
    public async Task<TournamentDetail?> Handle(GetTournament request, CancellationToken cancellationToken)
    {
        return await db.Tournaments
            .AsNoTracking()
            .Where(t => t.Id == request.TournamentId)
            .Select(t => new TournamentDetail(
                t.Id,
                t.Name,
                t.TeamSize,
                t.PointsToWin,
                t.ScoreSystem,
                t.MaxPlayerCount,
                t.IsArchived,
                t.IsPublic,
                t.SeedTournamentId,
                t.SeedTournament != null ? t.SeedTournament.Name : null,
                t.ParentTournamentId,
                t.ParentTournament != null ? t.ParentTournament.Name : null,
                t.RoundNumber,
                t.CreatedUtc,
                t.Players.Count,
                t.Matches.Count(m => m.State == MatchState.Done),
                t.Matches.Count(m => m.State == MatchState.Planned),
                t.Rounds
                    .OrderBy(r => r.RoundNumber)
                    .Select(r => new TournamentRound(r.Id, r.Name, r.RoundNumber ?? 1, r.Players.Count))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
