namespace Idasletten.Features.Tournaments.Entities;

public class TournamentTeamPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentTeamId { get; set; }
    public Guid TournamentPlayerId { get; set; }

    public TournamentTeam Team { get; set; } = null!;
    public TournamentPlayer Player { get; set; } = null!;
}
