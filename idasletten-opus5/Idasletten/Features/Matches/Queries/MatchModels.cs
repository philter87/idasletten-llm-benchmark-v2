namespace Idasletten.Features.Matches.Queries;

public record MatchPlayerRow(Guid UserId, string Initials, string DisplayName, string? ImageUrl);

public record MatchTeamRow(
    Guid TeamId,
    string Name,
    int Number,
    int Goals,
    IReadOnlyList<MatchPlayerRow> Players)
{
    public string PlayerInitials => string.Join(" + ", Players.Select(p => p.Initials));
}

public record MatchRow(
    Guid Id,
    int Order,
    MatchState State,
    DateTime? PlayedUtc,
    IReadOnlyList<MatchTeamRow> Teams)
{
    public string Title => string.Join(" vs ", Teams.Select(t => t.PlayerInitials));

    public string ScoreLine => State == MatchState.Done
        ? string.Join(" - ", Teams.Select(t => t.Goals))
        : "-";

    public IReadOnlyList<MatchTeamRow> Winners
    {
        get
        {
            if (State != MatchState.Done || Teams.Count == 0)
            {
                return [];
            }

            var best = Teams.Max(t => t.Goals);
            var winners = Teams.Where(t => t.Goals == best).ToList();
            return winners.Count == Teams.Count ? [] : winners;
        }
    }

    public bool IsWinner(MatchTeamRow team) => Winners.Any(w => w.TeamId == team.TeamId);
}
