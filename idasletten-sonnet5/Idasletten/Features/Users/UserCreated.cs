using MediatR;

namespace Idasletten.Features.Users;

public record UserCreated(Guid UserId, string Username) : INotification;
