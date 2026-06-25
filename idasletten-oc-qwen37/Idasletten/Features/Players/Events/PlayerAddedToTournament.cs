using MediatR;

namespace Idasletten.Features.Players.Events;

public record PlayerAddedToTournament(Guid PlayerId, Guid TournamentId) : INotification;
