using Idasletten.Shared.Entities;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;

namespace Idasletten.Features.Scoring;

/// <summary>
/// TrueSkill scoring using the Moserware.Skills library.
/// Score is stored as (Mean - 3*StdDev) * 100 to keep it comparable.
/// </summary>
public class TrueSkillScoringService : IScoringService
{
    private readonly GameInfo _gameInfo = GameInfo.DefaultGameInfo;

    public void CalculateScores(
        List<TournamentPlayer> team1Players,
        List<TournamentPlayer> team2Players,
        int team1Goals,
        int team2Goals,
        Tournament tournament)
    {
        // Build Moserware player/team structures
        var team1MoserPlayers = team1Players.Select(p => new Moserware.Skills.Player(p.Id)).ToList();
        var team2MoserPlayers = team2Players.Select(p => new Moserware.Skills.Player(p.Id)).ToList();

        var defaultRating = _gameInfo.DefaultRating;

        var moserTeam1 = new Team();
        foreach (var p in team1MoserPlayers)
            moserTeam1.AddPlayer(p, defaultRating);

        var moserTeam2 = new Team();
        foreach (var p in team2MoserPlayers)
            moserTeam2.AddPlayer(p, defaultRating);

        int rank1 = team1Goals >= team2Goals ? 1 : 2;
        int rank2 = team2Goals >= team1Goals ? 1 : 2;

        var newRatings = TrueSkillCalculator.CalculateNewRatings(
            _gameInfo,
            Teams.Concat(moserTeam1, moserTeam2),
            rank1, rank2);

        bool team1Won = team1Goals > team2Goals;
        bool team2Won = team2Goals > team1Goals;

        for (int i = 0; i < team1Players.Count; i++)
        {
            var player = team1Players[i];
            var moserPlayer = team1MoserPlayers[i];
            var newRating = newRatings[moserPlayer];
            double newScore = (newRating.Mean - 3 * newRating.StandardDeviation) * 100;
            player.ScoreDiff = newScore - player.Score;
            player.Score = newScore;
            player.MatchCount++;
            player.PointsWon += team1Goals;
            player.PointsLost += team2Goals;
            if (team1Won) player.WinCount++;
            else if (team2Won) player.LoseCount++;
        }

        for (int i = 0; i < team2Players.Count; i++)
        {
            var player = team2Players[i];
            var moserPlayer = team2MoserPlayers[i];
            var newRating = newRatings[moserPlayer];
            double newScore = (newRating.Mean - 3 * newRating.StandardDeviation) * 100;
            player.ScoreDiff = newScore - player.Score;
            player.Score = newScore;
            player.MatchCount++;
            player.PointsWon += team2Goals;
            player.PointsLost += team1Goals;
            if (team2Won) player.WinCount++;
            else if (team1Won) player.LoseCount++;
        }
    }
}
