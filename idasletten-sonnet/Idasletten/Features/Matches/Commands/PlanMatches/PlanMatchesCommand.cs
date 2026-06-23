using Idasletten.Shared.Enums;
using MediatR;

namespace Idasletten.Features.Matches.Commands.PlanMatches;

public record PlanMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId = null
) : IRequest<int>;
