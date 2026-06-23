using MediatR;

namespace Idasletten.Features.Players.Events;

public record PlayerAdded(Guid TournamentId, Guid UserId) : INotification;
