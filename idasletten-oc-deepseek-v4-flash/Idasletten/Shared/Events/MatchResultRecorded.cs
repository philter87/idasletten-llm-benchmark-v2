using MediatR;

namespace Idasletten.Shared.Events;

public record MatchResultRecorded(Guid MatchId, Guid TournamentId) : INotification;
