using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public class LivesScoreCalculator : IScoreCalculator
{
    public void UpdateScores(IList<TournamentPlayer> team1Players, IList<TournamentPlayer> team2Players,
        int team1Goals, int team2Goals, Tournament tournament)
    {
        var losingTeam = team1Goals < team2Goals ? team1Players : team2Players;
        var winningTeam = team1Goals < team2Goals ? team2Players : team1Players;

        foreach (var p in losingTeam)
        {
            var oldLives = p.Lives;
            p.Lives = Math.Max(0, p.Lives - 1);
            p.ScoreDiff = p.Lives - oldLives;
            p.Score = p.Lives;
            p.LoseCount++;
            p.MatchCount++;
        }

        foreach (var p in winningTeam)
        {
            p.ScoreDiff = 0;
            p.Score = p.Lives;
            p.WinCount++;
            p.MatchCount++;
        }

        foreach (var p in team1Players)
        {
            p.PointsWon += team1Goals;
            p.PointsLost += team2Goals;
        }

        foreach (var p in team2Players)
        {
            p.PointsWon += team2Goals;
            p.PointsLost += team1Goals;
        }
    }
}
