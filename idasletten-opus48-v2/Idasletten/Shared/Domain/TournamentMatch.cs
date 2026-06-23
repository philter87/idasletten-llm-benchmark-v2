namespace Idasletten.Shared.Domain;

/// <summary>A match between two (or more) teams in a tournament.</summary>
public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Sequence/display ordering.</summary>
    public int Order { get; set; }

    public MatchState State { get; set; } = MatchState.Planned;

    public List<TournamentTeamMatchResult> Results { get; set; } = new();
}
