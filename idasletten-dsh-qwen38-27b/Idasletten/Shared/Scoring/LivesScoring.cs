using Idasletten.Models;

namespace Idasletten.Scoring;

/// <summary>Lose a match → lose a life (3 to start). Score mirrors remaining lives.</summary>
public sealed class LivesScoring : IScoringEngine
{
    public const int StartingLives = 3;

    public ScoreSystem System => ScoreSystem.Lives;
    public double InitialScore => StartingLives;

    public void Initialize(TournamentPlayer player)
    {
        player.Lives = StartingLives;
        player.Score = StartingLives;
        player.ScoreDiff = 0;
    }

    public void Apply(TournamentPlayer[] players, int goals, IReadOnlyList<TeamResult> allTeams)
    {
        var self = allTeams.First(t => t.Players.Any(p => players.Any(q => ReferenceEquals(q, p))));
        if (MatchOutcomes.Lost(self, allTeams))
        {
            foreach (var p in players)
            {
                p.Lives = Math.Max(0, p.Lives - 1);
                p.Score = p.Lives;
                p.ScoreDiff = -1;
            }
        }
        else
        {
            foreach (var p in players)
            {
                p.Score = p.Lives;
                p.ScoreDiff = 0;
            }
        }
    }
}
