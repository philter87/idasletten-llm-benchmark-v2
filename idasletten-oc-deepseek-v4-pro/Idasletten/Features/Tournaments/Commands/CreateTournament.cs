using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsPublic,
    Guid? SeedTournamentId = null,
    Guid? ParentTournamentId = null,
    bool PlanMatchesAfterCreate = false
) : IRequest<Guid>;

public record TournamentCreated(Guid TournamentId) : INotification;
