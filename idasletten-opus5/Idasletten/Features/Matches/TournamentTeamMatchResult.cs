using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Matches;

/// <summary>
/// One team's line in one match. Also present while the match is only planned - then the goals are 0
/// and the row simply says which teams are going to meet.
/// </summary>
public class TournamentTeamMatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;

    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
}
