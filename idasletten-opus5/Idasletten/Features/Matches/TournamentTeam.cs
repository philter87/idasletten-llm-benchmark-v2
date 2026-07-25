using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Matches;

/// <summary>
/// One or more tournament players that play together. Teams are reused inside a tournament when the
/// exact same set of players plays again, which is what makes "fixed teams" work.
/// </summary>
public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Auto generated as "Team {Number}" - never a user input.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Auto incremented number within the tournament.</summary>
    public int Number { get; set; }

    public List<TournamentTeamPlayer> Players { get; set; } = [];
}

/// <summary>Join entity between a team and its tournament players.</summary>
public class TournamentTeamPlayer
{
    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;

    public Guid TournamentPlayerId { get; set; }
    public TournamentPlayer TournamentPlayer { get; set; } = null!;
}
