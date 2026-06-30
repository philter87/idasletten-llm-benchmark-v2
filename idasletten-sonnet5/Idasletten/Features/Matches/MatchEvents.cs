using MediatR;

namespace Idasletten.Features.Matches;

public record MatchPlanned(Guid MatchId, Guid TournamentId) : INotification;

public record MatchResultRecorded(Guid MatchId, Guid TournamentId) : INotification;

public record MatchesPlanned(Guid TournamentId, IReadOnlyList<Guid> MatchIds) : INotification;
