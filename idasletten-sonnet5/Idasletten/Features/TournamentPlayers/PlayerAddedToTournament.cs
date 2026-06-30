using MediatR;

namespace Idasletten.Features.TournamentPlayers;

public record PlayerAddedToTournament(Guid TournamentPlayerId, Guid TournamentId, Guid UserId) : INotification;
