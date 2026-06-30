using MediatR;

namespace Idasletten.Features.Tournaments.Queries.GetTournament;

public record GetTournamentQuery(Guid TournamentId) : IRequest<TournamentDto?>;

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
    int PlayerCount);
