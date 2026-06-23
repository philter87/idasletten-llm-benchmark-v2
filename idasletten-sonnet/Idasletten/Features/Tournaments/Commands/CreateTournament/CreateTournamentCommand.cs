using Idasletten.Shared.Enums;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public record CreateTournamentCommand(
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsPublic,
    Guid? ParentTournamentId = null,
    Guid? SeedTournamentId = null
) : IRequest<Guid>;
