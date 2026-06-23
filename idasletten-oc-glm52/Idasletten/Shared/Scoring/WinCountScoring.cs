using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;

namespace Idasletten.Shared.Scoring;

/// <summary>Score is simply the number of wins. Tie-breaker: goal difference (PointsWon - PointsLost).</summary>
public class WinCountScoring : IScoringSystem
{
    public void Initialise(TournamentPlayer player) => player.Score = 0;

    public void Apply(Tournament tournament, TournamentMatch match, ICollection<TournamentTeamMatchResult> results)
    {
        var resultByTeam = results.ToDictionary(r => r.TeamId);
        foreach (var team in match.Teams)
        {
            if (!resultByTeam.TryGetValue(team.Id, out var r)) continue;
            var won = r.GoalsWon > r.GoalsLost;
            foreach (var p in team.Players)
            {
                var prev = (int)p.Score;
                if (won) p.Score += 1;
                p.ScoreDiff = (int)p.Score - prev;
            }
        }
    }
}