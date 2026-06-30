using MediatR;

namespace Idasletten.Features.TournamentPlayers.Commands.RemovePlayerFromTournament;

public record RemovePlayerFromTournamentCommand(Guid TournamentId, Guid UserId) : IRequest;
