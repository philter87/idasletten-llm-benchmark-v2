using MediatR;

namespace Idasletten.Features.Players.Commands.AddPlayerToTournament;

public record PlayerAddedToTournament(Guid TournamentId, Guid TournamentPlayerId, Guid UserId) : INotification;
