namespace Idasletten.Shared.Entities;

public class TournamentMatchTeam
{
    public Guid MatchId { get; set; }
    public Guid TeamId { get; set; }

    public TournamentMatch Match { get; set; } = null!;
    public TournamentTeam Team { get; set; } = null!;
}
