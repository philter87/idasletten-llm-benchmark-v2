using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Matches;

public enum MatchState
{
    Planned = 0,
    Done = 1,
    Cancelled = 2,
}

/// <summary>A single game. Usually between two teams, but the model allows more.</summary>
public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Sequence/display ordering within the tournament.</summary>
    public int Order { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public MatchState State { get; set; } = MatchState.Planned;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the match got its result.</summary>
    public DateTime? PlayedUtc { get; set; }

    public List<TournamentTeamMatchResult> Results { get; set; } = [];

    public string Name => $"Match {Order}";
}
