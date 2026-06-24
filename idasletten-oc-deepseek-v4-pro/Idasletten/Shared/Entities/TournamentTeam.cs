namespace Idasletten.Shared.Entities;

public class TournamentTeam
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Number { get; set; }
    public Guid TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamPlayer> TeamPlayers { get; set; } = [];
    public ICollection<TournamentTeamMatchResult> MatchResults { get; set; } = [];
}
