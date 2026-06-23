namespace Idasletten.Shared.Domain;

/// <summary>A team within a tournament, composed of one or more tournament players.</summary>
public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Auto-generated, e.g. "Team 1". May be overridden in a future feature.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Auto-generated sequence number 1, 2, ...</summary>
    public int Number { get; set; }

    public List<TournamentPlayer> Players { get; set; } = new();
}
