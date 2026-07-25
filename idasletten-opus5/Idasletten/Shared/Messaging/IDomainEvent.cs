using MediatR;

namespace Idasletten.Shared.Messaging;

/// <summary>
/// Marker for the events every command handler publishes when it is done. Keeping them in one
/// hierarchy makes it possible to subscribe to "anything that happened" (see DomainEventLogger).
/// </summary>
public interface IDomainEvent : INotification;
