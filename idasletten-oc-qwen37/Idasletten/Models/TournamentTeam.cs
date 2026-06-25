using System.ComponentModel.DataAnnotations;

namespace Idasletten.Models;

public class TournamentTeam
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Number { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentTeamMatchResult> MatchResults { get; set; } = new List<TournamentTeamMatchResult>();
}
