using MediatR;

namespace Idasletten.Features.Matches.Commands.PlanMultipleMatches;

public record PlanMultipleMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId) : IRequest<IReadOnlyList<Guid>>;
