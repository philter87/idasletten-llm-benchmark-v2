using MediatR;

namespace Idasletten.Features.Users.Events;

public sealed record UserCreated(Guid UserId) : INotification;
