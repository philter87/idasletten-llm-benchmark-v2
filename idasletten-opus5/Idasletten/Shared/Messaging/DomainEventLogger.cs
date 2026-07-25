using MediatR;
using Microsoft.Extensions.Logging;

namespace Idasletten.Shared.Messaging;

/// <summary>Writes an audit line for every domain event that is published.</summary>
public class DomainEventLogger<TEvent>(ILogger<DomainEventLogger<TEvent>> logger)
    : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain event {Event}: {Payload}", typeof(TEvent).Name, notification);
        return Task.CompletedTask;
    }
}
