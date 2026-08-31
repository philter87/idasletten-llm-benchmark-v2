using Idasletten.Models;

namespace Idasletten.Scoring;

/// <summary>A team's participation in one match, as consumed by the scoring engines.</summary>
public sealed class TeamResult
{
    public required IReadOnlyList<TournamentPlayer> Players { get; init; }
    public required int Goals { get; init; }

    public int OpponentsGoals(IReadOnlyList<TeamResult> all) =>
        all.Where(t => !ReferenceEquals(t, this)).Sum(t => t.Goals);
}

/// <summary>Outcome helpers shared by all engines.</summary>
public static class MatchOutcomes
{
    /// <summary>True when this team strictly beat every other team.</summary>
    public static bool Won(TeamResult team, IReadOnlyList<TeamResult> all)
    {
        if (all.Count == 2)
            return team.Goals > all.First(t => !ReferenceEquals(t, team)).Goals;
        return all.All(t => ReferenceEquals(t, team) || team.Goals > t.Goals);
    }

    /// <summary>True when this team is strictly below the best score in the match.</summary>
    public static bool Lost(TeamResult team, IReadOnlyList<TeamResult> all)
    {
        int best = all.Max(t => t.Goals);
        return team.Goals < best;
    }

    /// <summary>1 = first, ties repeat the rank (e.g. 5,5,3 → 1,1,3).</summary>
    public static int Rank(TeamResult team, IReadOnlyList<TeamResult> all)
    {
        int distinct = all.Select(t => t.Goals).Distinct().OrderByDescending(g => g).ToList().IndexOf(team.Goals) + 1;
        return distinct;
    }
}
