namespace Idasletten.Models;

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Sequence/display ordering within the tournament.</summary>
    public int Order { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public MatchState State { get; set; } = MatchState.Planned;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Teams in this match (two or more).</summary>
    public ICollection<MatchTeam> TeamSlots { get; set; } = new List<MatchTeam>();
    public ICollection<TournamentTeamMatchResult> Results { get; set; } = new List<TournamentTeamMatchResult>();
}

/// <summary>Participation of a <see cref="TournamentTeam"/> in a <see cref="TournamentMatch"/>.</summary>
public class MatchTeam
{
    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;

    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;
}
