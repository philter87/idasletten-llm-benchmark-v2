using MediatR;

namespace Idasletten.Features.Matches.Events;

public record MatchCreated(Guid MatchId) : INotification;
