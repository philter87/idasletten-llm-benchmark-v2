using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using Moserware.Skills;

namespace Idasletten.Features.ScoreSystems;

public class TrueSkillScoring : IScoringSystem
{
    public ScoreSystem Type => ScoreSystem.TrueSkill;

    public void Calculate(List<Guid> team1PlayerIds, List<Guid> team2PlayerIds,
        int team1Score, int team2Score, List<TournamentPlayer> allPlayers)
    {
        var team1Players = allPlayers.Where(p => team1PlayerIds.Contains(p.UserId)).ToList();
        var team2Players = allPlayers.Where(p => team2PlayerIds.Contains(p.UserId)).ToList();

        var teams = new List<IDictionary<TournamentPlayer, Rating>>();

        var team1Dict = new Dictionary<TournamentPlayer, Rating>();
        foreach (var player in team1Players)
        {
            var rating = new Rating(player.Score > 0 ? player.Score : 25.0, 25.0 / 3.0);
            team1Dict[player] = rating;
        }
        teams.Add(team1Dict);

        var team2Dict = new Dictionary<TournamentPlayer, Rating>();
        foreach (var player in team2Players)
        {
            var rating = new Rating(player.Score > 0 ? player.Score : 25.0, 25.0 / 3.0);
            team2Dict[player] = rating;
        }
        teams.Add(team2Dict);

        var ranks = team1Score > team2Score
            ? new[] { 1, 2 }
            : team1Score < team2Score
                ? new[] { 2, 1 }
                : new[] { 1, 1 };

        var newRatings = TrueSkillCalculator.CalculateNewRatings(GameInfo.DefaultGameInfo, teams, ranks);

        foreach (var player in team1Players)
        {
            if (newRatings.TryGetValue(player, out var newRating))
                player.Score = newRating.ConservativeRating;
        }
        foreach (var player in team2Players)
        {
            if (newRatings.TryGetValue(player, out var newRating))
                player.Score = newRating.ConservativeRating;
        }
    }
}
