namespace Idasletten.Shared.Entities;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentTeamMatchResult> MatchResults { get; set; } = new List<TournamentTeamMatchResult>();
}
