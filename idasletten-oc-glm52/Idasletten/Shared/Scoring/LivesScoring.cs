using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;

namespace Idasletten.Shared.Scoring;

/// <summary>Lives scoring. Lose a match -> lose a life. Score stores remaining lives.</summary>
public class LivesScoring : IScoringSystem
{
    public void Initialise(TournamentPlayer player)
    {
        player.Lives = 3;
        player.Score = 3;
    }

    public void Apply(Tournament tournament, TournamentMatch match, ICollection<TournamentTeamMatchResult> results)
    {
        var resultByTeam = results.ToDictionary(r => r.TeamId);
        foreach (var team in match.Teams)
        {
            if (!resultByTeam.TryGetValue(team.Id, out var r)) continue;
            var lost = r.GoalsWon < r.GoalsLost;
            foreach (var p in team.Players)
            {
                var old = p.Lives;
                if (lost)
                {
                    p.Lives = Math.Max(0, p.Lives - 1);
                    p.Score = p.Lives;
                    p.ScoreDiff = p.Lives - old;
                }
                else
                {
                    p.ScoreDiff = 0;
                }
            }
        }
    }
}