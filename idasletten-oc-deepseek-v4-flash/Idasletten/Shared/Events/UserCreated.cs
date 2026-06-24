using MediatR;

namespace Idasletten.Shared.Events;

public record UserCreated(Guid UserId, string Username, string Name) : INotification;
