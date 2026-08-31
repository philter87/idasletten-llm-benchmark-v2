using MediatR;

namespace Idasletten.Features.Players.Events;

public sealed record PlayerAdded(Guid TournamentId, Guid TournamentPlayerId) : INotification;
public sealed record PlayerRemoved(Guid TournamentId, Guid TournamentPlayerId) : INotification;
