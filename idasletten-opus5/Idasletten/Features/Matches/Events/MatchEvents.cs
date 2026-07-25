using Idasletten.Shared.Messaging;

namespace Idasletten.Features.Matches.Events;

public record MatchPlanned(Guid TournamentId, Guid MatchId, int Order) : IDomainEvent;

public record MatchResultSaved(
    Guid TournamentId, Guid MatchId, int Order, bool WasAlreadyPlayed) : IDomainEvent;

public record MatchCancelled(Guid TournamentId, Guid MatchId) : IDomainEvent;

public record MatchesPlanned(
    Guid TournamentId, int MatchCount, SeedingType Seeding, bool FixedTeams) : IDomainEvent;
