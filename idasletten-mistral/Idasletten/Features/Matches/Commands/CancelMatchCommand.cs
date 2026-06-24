using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record CancelMatchCommand(Guid MatchId) : IRequest<Unit>;
