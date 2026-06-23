using Idasletten.Features.Matches.Entities;
using Idasletten.Features.Tournaments.Entities;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record PlayerDto(
    Guid Id,
    Guid UserId,
    string Username,
    string Name,
    double Score,
    int WinCount,
    int LoseCount,
    int MatchCount,
    int Lives,
    int PointsWon,
    int PointsLost,
    double ScoreDiff);

public record TournamentMatchPlayerDto(Guid TournamentPlayerId, Guid UserId, string Username, string Name);

public record TournamentMatchTeamDto(
    Guid TeamId,
    string TeamName,
    int TeamNumber,
    int Goals,
    int GoalsAgainst,
    bool Won,
    IReadOnlyList<string> PlayerUsernames,
    IReadOnlyList<TournamentMatchPlayerDto> Players);

public record TournamentMatchDto(
    Guid Id,
    Guid TournamentId,
    int Order,
    MatchState State,
    DateTimeOffset? PlayedAt,
    IReadOnlyList<TournamentMatchTeamDto> Teams);

public record TournamentDto(
    Guid Id,
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsArchived,
    bool IsPublic,
    Guid? SeedTournamentId,
    Guid? ParentTournamentId,
    int? RoundNumber,
    DateTimeOffset CreatedAt,
    int PlayerCount,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<TournamentMatchDto> Next5PlannedMatches,
    IReadOnlyList<TournamentMatchDto> Recent5DoneMatches);

public record GetTournamentQuery(Guid TournamentId) : IRequest<TournamentDto?>;

public sealed class GetTournamentHandler(AppDbContext db, IMediator mediator) : IRequestHandler<GetTournamentQuery, TournamentDto?>
{
    private readonly AppDbContext _db = db;

    public async Task<TournamentDto?> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == request.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return null;
        }

        var players = await _db.TournamentPlayers
            .AsNoTracking()
            .Where(player => player.TournamentId == request.TournamentId)
            .Include(player => player.User)
            .OrderByDescending(player => player.Score)
            .ThenBy(player => player.User.Username)
            .Select(player => new PlayerDto(
                player.Id,
                player.UserId,
                player.User.Username,
                player.User.Name,
                player.Score,
                player.WinCount,
                player.LoseCount,
                player.MatchCount,
                player.Lives,
                player.PointsWon,
                player.PointsLost,
                player.ScoreDiff))
            .ToListAsync(cancellationToken);

        var next5PlannedMatches = await _db.TournamentMatches
            .AsNoTracking()
            .Where(match => match.TournamentId == request.TournamentId && match.State == MatchState.Planned)
            .OrderBy(match => match.Order)
            .Take(5)
            .Include(match => match.TeamResults)
                .ThenInclude(result => result.Team)
                    .ThenInclude(team => team.TeamPlayers)
                        .ThenInclude(teamPlayer => teamPlayer.Player)
                            .ThenInclude(player => player.User)
            .ToListAsync(cancellationToken);

        var recent5DoneMatches = (await _db.TournamentMatches
            .AsNoTracking()
            .Where(match => match.TournamentId == request.TournamentId && match.State == MatchState.Done)
            .OrderByDescending(match => match.Order)
            .Take(10)
            .Include(match => match.TeamResults)
                .ThenInclude(result => result.Team)
                    .ThenInclude(team => team.TeamPlayers)
                        .ThenInclude(teamPlayer => teamPlayer.Player)
                            .ThenInclude(player => player.User)
            .ToListAsync(cancellationToken))
            .OrderByDescending(match => match.PlayedAt)
            .ThenByDescending(match => match.Order)
            .Take(5)
            .ToList();

        return new TournamentDto(
            tournament.Id,
            tournament.Name,
            tournament.TeamSize,
            tournament.PointsToWin,
            tournament.ScoreSystem,
            tournament.MaxPlayerCount,
            tournament.IsArchived,
            tournament.IsPublic,
            tournament.SeedTournamentId,
            tournament.ParentTournamentId,
            tournament.RoundNumber,
            tournament.CreatedAt,
            players.Count,
            players,
            next5PlannedMatches.Select(MapMatch).ToList(),
            recent5DoneMatches.Select(MapMatch).ToList());
    }

    private static TournamentMatchDto MapMatch(TournamentMatch match)
    {
        var winningGoals = match.TeamResults.Count == 0 ? 0 : match.TeamResults.Max(result => result.GoalsWon);

        return new TournamentMatchDto(
            match.Id,
            match.TournamentId,
            match.Order,
            match.State,
            match.PlayedAt,
            match.TeamResults
                .OrderBy(result => result.Team.Number)
                .Select(result => new TournamentMatchTeamDto(
                    result.TeamId,
                    result.Team.Name,
                    result.Team.Number,
                    result.GoalsWon,
                    result.GoalsLost,
                    match.State == MatchState.Done && result.GoalsWon == winningGoals,
                    result.Team.TeamPlayers
                        .OrderBy(teamPlayer => teamPlayer.Player.User.Username)
                        .Select(teamPlayer => teamPlayer.Player.User.Username)
                        .ToList(),
                    result.Team.TeamPlayers
                        .OrderBy(teamPlayer => teamPlayer.Player.User.Username)
                        .Select(teamPlayer => new TournamentMatchPlayerDto(
                            teamPlayer.Player.Id,
                            teamPlayer.Player.UserId,
                            teamPlayer.Player.User.Username,
                            teamPlayer.Player.User.Name))
                        .ToList()))
                .ToList());
    }
}
