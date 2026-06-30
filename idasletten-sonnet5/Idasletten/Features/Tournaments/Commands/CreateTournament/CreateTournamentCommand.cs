using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public record CreateTournamentCommand(
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsPublic,
    Guid? SeedTournamentId = null,
    Guid? ParentTournamentId = null,
    int? RoundNumber = null) : IRequest<Guid>;
