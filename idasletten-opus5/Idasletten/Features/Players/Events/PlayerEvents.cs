using Idasletten.Shared.Messaging;

namespace Idasletten.Features.Players.Events;

public record PlayerAddedToTournament(
    Guid TournamentId, Guid TournamentPlayerId, Guid UserId, string Initials) : IDomainEvent;

public record PlayersAddedFromTournament(
    Guid TournamentId, Guid SourceTournamentId, int PlayerCount) : IDomainEvent;

public record PlayerRemovedFromTournament(Guid TournamentId, Guid TournamentPlayerId) : IDomainEvent;
