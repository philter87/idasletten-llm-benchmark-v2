using Idasletten.Shared.Entities;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;

namespace Idasletten.Features.Scoring;

public class TrueSkillScoreCalculator : IScoreCalculator
{
    public void UpdateScores(IList<TournamentPlayer> team1Players, IList<TournamentPlayer> team2Players,
        int team1Goals, int team2Goals, Tournament tournament)
    {
        var gameInfo = GameInfo.DefaultGameInfo;
        var calc = new TwoTeamTrueSkillCalculator();

        // Build rating lookup: userId → (Player, old Rating)
        var ratings1 = team1Players.ToDictionary(
            p => p.UserId,
            p => (player: new Player(p.UserId), rating: new Rating(p.Score, gameInfo.InitialStandardDeviation)));

        var ratings2 = team2Players.ToDictionary(
            p => p.UserId,
            p => (player: new Player(p.UserId), rating: new Rating(p.Score, gameInfo.InitialStandardDeviation)));

        // Build Team objects
        Team<Player> t1 = BuildTeam(ratings1.Values.ToList());
        Team<Player> t2 = BuildTeam(ratings2.Values.ToList());

        var teams = Teams.Concat(t1, t2);
        int[] ranks = team1Goals > team2Goals ? [1, 2] : team1Goals < team2Goals ? [2, 1] : [1, 1];

        var newRatings = calc.CalculateNewRatings(gameInfo, teams, ranks);

        foreach (var p in team1Players)
        {
            var newRating = newRatings[ratings1[p.UserId].player];
            p.ScoreDiff = newRating.Mean - p.Score;
            p.Score = newRating.Mean;
            p.MatchCount++;
            p.PointsWon += team1Goals;
            p.PointsLost += team2Goals;
            if (team1Goals > team2Goals) p.WinCount++;
            else if (team1Goals < team2Goals) p.LoseCount++;
        }

        foreach (var p in team2Players)
        {
            var newRating = newRatings[ratings2[p.UserId].player];
            p.ScoreDiff = newRating.Mean - p.Score;
            p.Score = newRating.Mean;
            p.MatchCount++;
            p.PointsWon += team2Goals;
            p.PointsLost += team1Goals;
            if (team2Goals > team1Goals) p.WinCount++;
            else if (team2Goals < team1Goals) p.LoseCount++;
        }
    }

    private static Team<Player> BuildTeam(IList<(Player player, Rating rating)> entries)
    {
        var team = new Team<Player>(entries[0].player, entries[0].rating);
        for (int i = 1; i < entries.Count; i++)
            team.AddPlayer(entries[i].player, entries[i].rating);
        return team;
    }
}
