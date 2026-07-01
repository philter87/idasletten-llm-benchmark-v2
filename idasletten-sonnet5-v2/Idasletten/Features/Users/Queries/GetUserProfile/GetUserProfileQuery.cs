using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileResult?>;

public record UserProfileTournamentStat(
    Guid TournamentId,
    string TournamentName,
    ScoreSystem ScoreSystem,
    double Score,
    int WinCount,
    int LoseCount,
    int MatchCount,
    int PointsWon,
    int PointsLost);

public record UserProfileResult(
    Guid UserId,
    string Username,
    string Name,
    string? ImageUrl,
    IReadOnlyList<UserProfileTournamentStat> Tournaments);
