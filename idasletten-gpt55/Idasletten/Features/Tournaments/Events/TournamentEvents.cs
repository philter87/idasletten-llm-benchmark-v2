using MediatR;

namespace Idasletten.Features.Tournaments.Events;

public record TournamentCreated(Guid TournamentId) : INotification;
public record TournamentPlayerAdded(Guid TournamentId, Guid UserId) : INotification;
public record MatchRecorded(Guid TournamentId, Guid MatchId) : INotification;
public record PlannedMatchCreated(Guid TournamentId, Guid MatchId) : INotification;
