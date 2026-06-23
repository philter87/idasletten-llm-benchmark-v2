using MediatR;

namespace Idasletten.Features.Matches.Commands.CreateMatch;

public record TeamInput(IList<string> PlayerInitials, int Goals);

public record CreateMatchCommand(
    Guid TournamentId,
    TeamInput Team1,
    TeamInput Team2,
    Guid? ExistingMatchId = null
) : IRequest<Guid>;
