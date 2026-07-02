namespace Idasletten.Features.Matches;

public enum MatchState
{
    Planned,
    Done,
    Cancelled
}

/// <summary>Has two or more teams (usually two) via TournamentTeamMatchResult.</summary>
public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }

    /// <summary>Sequence/display ordering.</summary>
    public int Order { get; set; }

    public MatchState State { get; set; } = MatchState.Planned;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PlayedAt { get; set; }

    public List<TournamentTeamMatchResult> Results { get; set; } = [];
}
