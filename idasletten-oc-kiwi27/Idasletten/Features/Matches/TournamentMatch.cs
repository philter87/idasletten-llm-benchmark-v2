namespace Idasletten.Features.Matches;

public enum MatchState
{
    Planned,
    Done,
    Cancelled
}

public class TournamentMatch
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public Tournaments.Tournament Tournament { get; set; } = null!;
    public MatchState State { get; set; } = MatchState.Planned;
    public DateTime? CompletedAt { get; set; }

    public ICollection<TournamentTeam> Teams { get; set; } = [];
}
