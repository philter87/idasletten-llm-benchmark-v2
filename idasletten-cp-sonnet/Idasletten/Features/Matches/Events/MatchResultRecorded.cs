using MediatR;

namespace Idasletten.Features.Matches.Events;

public record MatchResultRecorded(Guid MatchId, Guid TournamentId) : INotification;
