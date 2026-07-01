using MediatR;

namespace Idasletten.Features.Users.Commands.CreateUser;

public record UserCreated(Guid UserId) : INotification;
