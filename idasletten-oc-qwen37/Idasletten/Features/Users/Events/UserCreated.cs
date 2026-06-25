using MediatR;

namespace Idasletten.Features.Users.Events;

public record UserCreated(Guid UserId) : INotification;
