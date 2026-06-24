using Idasletten.Shared.Entities;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;

namespace Idasletten.Features.Scoring;

public class TrueSkillScoringService : IScoringService
{
    private readonly GameInfo _gameInfo = GameInfo.DefaultGameInfo;

    public void Calculate(
        TournamentMatch match,
        IReadOnlyList<TournamentTeamMatchResult> teamResults,
        IReadOnlyList<TournamentTeam> teams,
        IReadOnlyList<TournamentPlayer> players)
    {
        var team1Result = teamResults[0];
        var team2Result = teamResults[1];
        var team1 = teams.Single(t => t.Id == team1Result.TeamId);
        var team2 = teams.Single(t => t.Id == team2Result.TeamId);

        var team1Players = GetTeamPlayers(team1, players);
        var team2Players = GetTeamPlayers(team2, players);

        var team1MoserPlayers = team1Players.Select(p => new Moserware.Skills.Player(p.Id)).ToList();
        var team2MoserPlayers = team2Players.Select(p => new Moserware.Skills.Player(p.Id)).ToList();

        var defaultRating = _gameInfo.DefaultRating;

        var moserTeam1 = new Moserware.Skills.Team();
        foreach (var p in team1MoserPlayers)
            moserTeam1.AddPlayer(p, defaultRating);

        var moserTeam2 = new Moserware.Skills.Team();
        foreach (var p in team2MoserPlayers)
            moserTeam2.AddPlayer(p, defaultRating);

        int rank1 = team1Result.GoalsWon >= team2Result.GoalsWon ? 1 : 2;
        int rank2 = team2Result.GoalsWon >= team1Result.GoalsWon ? 1 : 2;

        var newRatings = TrueSkillCalculator.CalculateNewRatings(
            _gameInfo,
            Teams.Concat(moserTeam1, moserTeam2),
            rank1, rank2);

        foreach (var (player, moserPlayer) in team1Players.Zip(team1MoserPlayers))
        {
            var newRating = newRatings[moserPlayer];
            double newScore = (newRating.Mean - 3 * newRating.StandardDeviation) * 100;
            player.ScoreDiff = newScore - player.Score;
            player.Score = newScore;
        }

        foreach (var (player, moserPlayer) in team2Players.Zip(team2MoserPlayers))
        {
            var newRating = newRatings[moserPlayer];
            double newScore = (newRating.Mean - 3 * newRating.StandardDeviation) * 100;
            player.ScoreDiff = newScore - player.Score;
            player.Score = newScore;
        }
    }

    private static List<TournamentPlayer> GetTeamPlayers(TournamentTeam team, IReadOnlyList<TournamentPlayer> allPlayers)
    {
        return team.TeamPlayers
            .Select(tp => allPlayers.Single(p => p.Id == tp.TournamentPlayerId))
            .ToList();
    }
}
