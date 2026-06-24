using Idasletten.Shared.Data.Enums;
using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record PlanMatchesCommand(
    Guid TournamentId,
    Guid? SeedTournamentId,
    int GamesPerPlayer,
    bool FixedTeam,
    SeedingType SeedingType) : IRequest<List<Guid>>;
