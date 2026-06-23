using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Teams;

namespace Idasletten.Shared.Scoring;

public interface IScoringSystem
{
    void Apply(Tournament tournament, TournamentMatch match, ICollection<TournamentTeamMatchResult> results);
    void Initialise(TournamentPlayer player);
}

/// <summary>ELO with team averaging. Default mean 1200.</summary>
public class EloScoring : IScoringSystem
{
    private const double KFactor = 32;
    private const double Initial = 1200;

    public void Initialise(TournamentPlayer player) => player.Score = Initial;

    public void Apply(Tournament tournament, TournamentMatch match, ICollection<TournamentTeamMatchResult> results)
    {
        var resultByTeam = results.ToDictionary(r => r.TeamId);
        var teams = match.Teams.ToList();
        if (teams.Count != 2) return;

        var teamA = teams[0];
        var teamB = teams[1];
        if (!resultByTeam.TryGetValue(teamA.Id, out var ra) || !resultByTeam.TryGetValue(teamB.Id, out var rb)) return;

        var avgA = teamA.Players.Count == 0 ? Initial : teamA.Players.Average(p => p.Score);
        var avgB = teamB.Players.Count == 0 ? Initial : teamB.Players.Average(p => p.Score);

        var expectedA = 1 / (1 + Math.Pow(10, (avgB - avgA) / 400));
        var expectedB = 1 - expectedA;

        double scoreA = ra.GoalsWon > ra.GoalsLost ? 1 : (ra.GoalsWon == ra.GoalsLost ? 0.5 : 0);
        // Scale delta by goal differential capped at ±K to allow lopsided matches to count more.
        var diffMagnitude = Math.Min(Math.Abs(ra.GoalsWon - rb.GoalsLost), 5);
        var scale = 0.5 + 0.1 * diffMagnitude;

        foreach (var p in teamA.Players)
        {
            var delta = KFactor * scale * (scoreA - expectedA);
            p.ScoreDiff = (int)Math.Round(delta);
            p.Score += delta;
        }
        foreach (var p in teamB.Players)
        {
            var delta = KFactor * scale * (1 - scoreA - expectedB);
            p.ScoreDiff = (int)Math.Round(delta);
            p.Score += delta;
        }
    }
}