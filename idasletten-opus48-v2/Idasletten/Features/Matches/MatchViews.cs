using Idasletten.Shared.Domain;

namespace Idasletten.Features.Matches;

/// <summary>One team's line in a match summary.</summary>
public record TeamView(Guid TeamId, string Name, IReadOnlyList<string> PlayerInitials, int Goals);

/// <summary>A match rendered for lists and detail (cards, tables).</summary>
public record MatchView(Guid Id, int Order, MatchState State, IReadOnlyList<TeamView> Teams)
{
    public bool IsDraw => Teams.Count > 1 && Teams.All(t => t.Goals == Teams[0].Goals);
    public TeamView? Winner => IsDraw ? null : Teams.MaxBy(t => t.Goals);
}
