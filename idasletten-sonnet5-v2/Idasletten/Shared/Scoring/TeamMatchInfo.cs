using Idasletten.Shared.Entities;

namespace Idasletten.Shared.Scoring;

public sealed class TeamMatchInfo
{
    public required Guid TeamId { get; init; }
    public required IReadOnlyList<TournamentPlayer> Players { get; init; }
    public required int GoalsWon { get; init; }
    public required int GoalsLost { get; init; }
}
