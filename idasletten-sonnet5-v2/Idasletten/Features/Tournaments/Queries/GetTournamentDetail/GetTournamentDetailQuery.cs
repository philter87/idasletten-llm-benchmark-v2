using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries.GetTournamentDetail;

public record GetTournamentDetailQuery(Guid TournamentId) : IRequest<TournamentDetailResult?>;

public record TournamentDetailPlayerDto(
    Guid TournamentPlayerId,
    Guid UserId,
    string Username,
    string Name,
    string? ImageUrl,
    double Score,
    double ScoreDiff,
    int WinCount,
    int LoseCount,
    int MatchCount,
    int Lives,
    int PointsWon,
    int PointsLost);

public record TournamentDetailMatchTeamDto(
    Guid TeamId,
    string Name,
    IReadOnlyList<string> PlayerUsernames,
    int? GoalsWon,
    int? GoalsLost);

public record TournamentDetailMatchDto(
    Guid MatchId,
    int Order,
    MatchState State,
    IReadOnlyList<TournamentDetailMatchTeamDto> Teams);

public record TournamentDetailResult(
    Guid TournamentId,
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
    IReadOnlyList<TournamentDetailPlayerDto> Scoreboard,
    IReadOnlyList<TournamentDetailMatchDto> NextPlannedMatches,
    IReadOnlyList<TournamentDetailMatchDto> RecentPlayedMatches);
