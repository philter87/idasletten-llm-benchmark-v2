using Idasletten.Shared.Data.Enums;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    int TeamSize = 2,
    int PointsToWin = 5,
    ScoreSystem ScoreSystem = ScoreSystem.TrueSkill,
    int? MaxPlayerCount = null,
    bool IsPublic = true) : IRequest<Guid>;
