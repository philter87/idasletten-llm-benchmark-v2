using Idasletten.Shared.Data.Enums;
using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record RecordMatchResultCommand(
    Guid MatchId,
    Guid TournamentId,
    List<Guid> TeamIds,
    List<int> GoalsScored) : IRequest<Unit>;
