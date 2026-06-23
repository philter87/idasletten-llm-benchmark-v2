using MediatR;

namespace Idasletten.Shared.Events;

/// <summary>
/// Default subscriber so every published domain event has at least one handler. Keeps an audit
/// trail in the logs; feature-specific reactions add their own INotificationHandler.
/// </summary>
public class LoggingEventHandler<TEvent> : INotificationHandler<TEvent> where TEvent : IDomainEvent
{
    private readonly ILogger<LoggingEventHandler<TEvent>> _logger;

    public LoggingEventHandler(ILogger<LoggingEventHandler<TEvent>> logger) => _logger = logger;

    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain event: {Event}", notification.GetType().Name);
        return Task.CompletedTask;
    }
}
