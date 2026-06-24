namespace Idasletten.Shared.Entities;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // Auto-generated: "Team 1", "Team 2"
    public int Number { get; set; } // Auto-generated: 1, 2, ...
    public Guid TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamPlayer> TeamPlayers { get; set; } = new List<TournamentTeamPlayer>();
    public ICollection<TournamentTeamMatchResult> MatchResults { get; set; } = new List<TournamentTeamMatchResult>();
}
