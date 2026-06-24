namespace Idasletten.Features.Matches.Queries;

public class TeamListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
    public List<string> Members { get; set; } = [];
}

public class MatchListItemDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public MatchState State { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<TeamListItemDto> Teams { get; set; } = [];
}
