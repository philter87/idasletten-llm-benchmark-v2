namespace Idasletten.Shared.Entities;

public enum MatchState
{
    Planned,
    Done,
    Cancelled
}

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public MatchState State { get; set; } = MatchState.Planned;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PlayedAt { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamMatchResult> TeamResults { get; set; } = new List<TournamentTeamMatchResult>();
}
