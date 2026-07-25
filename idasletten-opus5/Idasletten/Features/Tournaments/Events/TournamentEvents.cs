using Idasletten.Shared.Messaging;

namespace Idasletten.Features.Tournaments.Events;

public record TournamentCreated(
    Guid TournamentId,
    string Name,
    ScoreSystem ScoreSystem,
    Guid? ParentTournamentId,
    int? RoundNumber) : IDomainEvent;

public record SeedTournamentSet(Guid TournamentId, Guid SeedTournamentId) : IDomainEvent;

public record TournamentArchiveChanged(Guid TournamentId, bool IsArchived) : IDomainEvent;
