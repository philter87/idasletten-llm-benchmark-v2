using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record CreateMatchCommand(
    Guid TournamentId,
    int Order,
    List<TeamResultDto> TeamResults
) : IRequest<Guid>;

public record TeamResultDto(List<string> PlayerInitials, int GoalsWon);
