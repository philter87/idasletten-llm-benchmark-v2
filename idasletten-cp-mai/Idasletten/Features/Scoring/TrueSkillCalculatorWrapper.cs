using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Moserware.Skills;

namespace Idasletten.Features.Scoring;

public class TrueSkillCalculatorWrapper : IScoreCalculator
{
    private static readonly GameInfo GameInfo = GameInfo.DefaultGameInfo;

    public double InitialScore => GameInfo.DefaultRating.ConservativeRating;

    public void ApplyMatch(Tournament tournament, Dictionary<Guid, TournamentPlayer> playersByUserId, TournamentMatch match)
    {
        var teams = match.Teams.ToList();
        if (teams.Count != 2) return;

        var teamA = teams[0];
        var teamB = teams[1];

        var keysA = new List<Moserware.Skills.Player>();
        var skillTeamA = new Team();
        foreach (var m in teamA.Members)
        {
            var p = playersByUserId[m.UserId];
            var key = new Moserware.Skills.Player(m.UserId);
            keysA.Add(key);
            skillTeamA = (Team)skillTeamA.AddPlayer(key, new Rating(p.TrueSkillMean, p.TrueSkillStdDev));
        }

        var keysB = new List<Moserware.Skills.Player>();
        var skillTeamB = new Team();
        foreach (var m in teamB.Members)
        {
            var p = playersByUserId[m.UserId];
            var key = new Moserware.Skills.Player(m.UserId);
            keysB.Add(key);
            skillTeamB = (Team)skillTeamB.AddPlayer(key, new Rating(p.TrueSkillMean, p.TrueSkillStdDev));
        }

        var concat = Teams.Concat(skillTeamA, skillTeamB);

        int rankA, rankB;
        if (teamA.GoalsWon > teamB.GoalsWon) { rankA = 1; rankB = 2; }
        else if (teamA.GoalsWon < teamB.GoalsWon) { rankA = 2; rankB = 1; }
        else { rankA = 1; rankB = 1; }

        var newRatings = Moserware.Skills.TrueSkillCalculator.CalculateNewRatings(GameInfo, concat, rankA, rankB);

        UpdateTeam(teamA, keysA, teamB.GoalsWon, newRatings, playersByUserId);
        UpdateTeam(teamB, keysB, teamA.GoalsWon, newRatings, playersByUserId);
    }

    private static void UpdateTeam(TournamentTeam team, List<Moserware.Skills.Player> keys, int opponentGoals,
        IDictionary<Moserware.Skills.Player, Rating> newRatings,
        Dictionary<Guid, TournamentPlayer> playersByUserId)
    {
        int i = 0;
        foreach (var member in team.Members)
        {
            var player = playersByUserId[member.UserId];
            var rating = newRatings[keys[i++]];
            double before = player.Score;
            player.TrueSkillMean = rating.Mean;
            player.TrueSkillStdDev = rating.StandardDeviation;
            player.Score = rating.ConservativeRating;
            player.ScoreDiff = player.Score - before;
            player.PointsWon += team.GoalsWon;
            player.PointsLost += opponentGoals;
            player.MatchCount++;
            bool won = team.GoalsWon > opponentGoals;
            bool lost = team.GoalsWon < opponentGoals;
            if (won) player.WinCount++;
            else if (lost) player.LoseCount++;
        }
    }
}
