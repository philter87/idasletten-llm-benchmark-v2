using MediatR;

namespace Idasletten.Features.Users.Queries.GetUser;

public record GetUserQuery(Guid UserId) : IRequest<UserDetailDto?>;

public record UserDetailDto(
    Guid Id,
    string Username,
    string Name,
    string? ImageUrl,
    IReadOnlyList<UserTournamentStatsDto> Tournaments);

public record UserTournamentStatsDto(
    Guid TournamentId,
    string TournamentName,
    double Score,
    int WinCount,
    int LoseCount,
    int MatchCount,
    int PointsWon,
    int PointsLost);
