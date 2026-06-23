namespace Idasletten.Features.Tournaments.Entities;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public string Name { get; set; } = string.Empty; // Auto-generated: "Team 1", "Team 2", etc.
    public int Number { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public ICollection<TournamentTeamPlayer> TeamPlayers { get; set; } = new List<TournamentTeamPlayer>();
    public ICollection<Matches.Entities.TournamentTeamMatchResult> MatchResults { get; set; } = new List<Matches.Entities.TournamentTeamMatchResult>();
}
