using MediatR;

namespace Idasletten.Shared.Events;

public record TournamentCreated(Guid TournamentId, string Name) : INotification;
