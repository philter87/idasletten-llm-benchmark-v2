using MediatR;

namespace Idasletten.Features.Matches.Events;

public sealed record MatchPlanned(Guid MatchId, Guid TournamentId) : INotification;
public sealed record MatchesPlanned(Guid TournamentId, int Count) : INotification;
public sealed record MatchResultRecorded(Guid MatchId, Guid TournamentId, bool IsNew) : INotification;
public sealed record MatchCancelled(Guid MatchId, Guid TournamentId) : INotification;
