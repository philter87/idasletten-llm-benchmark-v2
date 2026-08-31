using Idasletten.Models;

namespace Idasletten.Features.Tournaments;

public sealed record TournamentCardDto(Guid Id, string Name, ScoreSystem ScoreSystem, int TeamSize, int PointsToWin,
    bool IsPublic, bool IsArchived, bool IsChild, int? RoundNumber, int PlayerCount);

public sealed record PlayerRowDto(Guid TournamentPlayerId, Guid UserId, string Initials, string Name, string? Email,
    string? ImageUrl, double Score, double ScoreDiff, int WinCount, int LoseCount, int MatchCount,
    int PointsWon, int PointsLost, int Lives);

public sealed record TeamSummaryDto(Guid TeamId, string Name, int Number, int? Goals,
    IReadOnlyList<PlayerCellDto> Players);

public sealed record PlayerCellDto(Guid TournamentPlayerId, Guid UserId, string Initials, string Name);

public sealed record MatchSummaryDto(Guid MatchId, int Order, MatchState State,
    IReadOnlyList<TeamSummaryDto> Teams);

public sealed record TournamentDetailDto(
    Guid Id, string Name, ScoreSystem ScoreSystem, int TeamSize, int PointsToWin,
    int? MaxPlayerCount, bool IsPublic, bool IsArchived, bool IsChild, int? RoundNumber,
    string? ParentTournamentName, int? RoundNumberDisplay,
    Guid? SeedTournamentId, string? SeedTournamentName,
    int PlayerCount,
    IReadOnlyList<PlayerRowDto> Players,
    IReadOnlyList<MatchSummaryDto> NextPlannedMatches,
    IReadOnlyList<MatchSummaryDto> RecentPlayedMatches);
