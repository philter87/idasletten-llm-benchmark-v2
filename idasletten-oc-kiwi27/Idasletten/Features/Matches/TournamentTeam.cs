namespace Idasletten.Features.Matches;

public class TournamentTeam
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;
    public Guid TournamentId { get; set; }
    public Tournaments.Tournament Tournament { get; set; } = null!;
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }

    public ICollection<Players.TournamentPlayer> Members { get; set; } = [];
}
