using MediatR;

namespace Idasletten.Features.Players.Commands;

public record RemovePlayerFromTournamentCommand(Guid TournamentId, Guid PlayerId) : IRequest<Unit>;
