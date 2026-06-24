namespace Idasletten.Shared.Entities;

public class TournamentTeamPlayer
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public Guid TournamentId { get; set; }

    public TournamentTeam Team { get; set; } = null!;
    public TournamentPlayer Player { get; set; } = null!;
}
