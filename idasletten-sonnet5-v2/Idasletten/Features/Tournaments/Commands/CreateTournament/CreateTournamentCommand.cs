using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public record CreateTournamentCommand(
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsPublic) : IRequest<Guid>;
