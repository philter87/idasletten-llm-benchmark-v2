using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Matches.Commands;

public enum SeedingType
{
    Random,
    Equality,
    Fair
}

public record PlanMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId
) : IRequest;
