using MediatR;

namespace Idasletten.Shared.Events;

/// <summary>
/// Marker for events published by command handlers. The architecture rule is that every command
/// handler publishes one of these at the end (e.g. CreateUserHandler → UserCreated).
/// </summary>
public interface IDomainEvent : INotification;
