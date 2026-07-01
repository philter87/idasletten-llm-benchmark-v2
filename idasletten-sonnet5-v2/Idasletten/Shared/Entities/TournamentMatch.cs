namespace Idasletten.Shared.Entities;

public class TournamentMatch
{
    public Guid Id { get; set; }

    /// <summary>Sequence/display ordering.</summary>
    public int Order { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public MatchState State { get; set; } = MatchState.Planned;

    public List<TournamentTeam> Teams { get; set; } = [];
    public List<TournamentTeamMatchResult> Results { get; set; } = [];
}
