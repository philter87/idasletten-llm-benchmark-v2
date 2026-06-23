namespace Idasletten.Features.Tournaments;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public Guid TournamentId { get; set; }

    // Navigation properties
    public virtual Tournament Tournament { get; set; } = null!;
    public virtual ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public virtual ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
}
