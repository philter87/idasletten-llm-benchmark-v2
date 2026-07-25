using Idasletten.Shared.Messaging;

namespace Idasletten.Features.Users.Events;

public record UserCreated(Guid UserId, string Initials, string? Email) : IDomainEvent;
