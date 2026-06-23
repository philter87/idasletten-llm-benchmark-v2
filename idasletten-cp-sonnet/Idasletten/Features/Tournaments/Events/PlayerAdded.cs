using MediatR;

namespace Idasletten.Features.Tournaments.Events;

public record PlayerAdded(Guid TournamentId, Guid UserId) : INotification;
