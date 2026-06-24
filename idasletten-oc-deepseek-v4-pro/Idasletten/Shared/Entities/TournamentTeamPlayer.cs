namespace Idasletten.Shared.Entities;

public class TournamentTeamPlayer
{
    public Guid TournamentTeamId { get; set; }
    public Guid TournamentPlayerId { get; set; }

    public TournamentTeam Team { get; set; } = null!;
    public TournamentPlayer Player { get; set; } = null!;
}
