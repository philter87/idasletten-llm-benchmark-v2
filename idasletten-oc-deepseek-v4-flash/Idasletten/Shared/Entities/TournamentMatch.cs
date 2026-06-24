namespace Idasletten.Shared.Entities;

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public Enums.MatchState State { get; set; } = Enums.MatchState.Planned;

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentMatchTeam> TeamEntries { get; set; } = new List<TournamentMatchTeam>();
    public ICollection<TournamentTeamMatchResult> Results { get; set; } = new List<TournamentTeamMatchResult>();
}
