using MediatR;

namespace Idasletten.Features.Matches.Commands;

public record RecordMatchResultCommand(
    Guid TournamentId,
    Guid? MatchId,
    string Team1Player1Initials,
    string? Team1Player2Initials,
    string Team2Player1Initials,
    string? Team2Player2Initials,
    int Team1Goals,
    int Team2Goals
) : IRequest<Guid>;

public record CreatePlannedMatchCommand(
    Guid TournamentId,
    string Team1Player1Initials,
    string? Team1Player2Initials,
    string Team2Player1Initials,
    string? Team2Player2Initials
) : IRequest<Guid>;

public enum SeedingType { Random, Equality, Fair }

public record PlanSeveralMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams,
    SeedingType SeedingType,
    Guid? SeedTournamentId = null
) : IRequest<int>;

public record MatchResultRecorded(Guid MatchId, Guid TournamentId) : INotification;
public record PlannedMatchCreated(Guid MatchId, Guid TournamentId) : INotification;
public record SeveralMatchesPlanned(Guid TournamentId, int Count) : INotification;
