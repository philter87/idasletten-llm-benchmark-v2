using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Teams;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamMatchResult> Results { get; set; } = new List<TournamentTeamMatchResult>();
}