namespace Idasletten.Shared.Entities;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public Guid TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamPlayer> PlayerEntries { get; set; } = new List<TournamentTeamPlayer>();
    public ICollection<TournamentMatchTeam> MatchEntries { get; set; } = new List<TournamentMatchTeam>();
    public ICollection<TournamentTeamMatchResult> Results { get; set; } = new List<TournamentTeamMatchResult>();
}
