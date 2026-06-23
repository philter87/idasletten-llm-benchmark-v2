using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Idasletten.Shared.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared;

public class ScoringSystemSelector
{
    public IScoringSystem For(Tournament t) => t.ScoreSystem switch
    {
        ScoreSystem.Elo => new EloScoring(),
        ScoreSystem.TrueSkill => new TrueSkillScoring(),
        ScoreSystem.Lives => new LivesScoring(),
        ScoreSystem.WinCount => new WinCountScoring(),
        _ => throw new ArgumentOutOfRangeException()
    };
}

public class MatchRecorder
{
    private readonly IdaslettenDbContext _db;
    public MatchRecorder(IdaslettenDbContext db) => _db = db;

    /// <summary>
    /// Applies a completed match result: writes TournamentTeamMatchResult rows, updates
    /// WinCount/MatchCount/LoseCount/PointsWon/PointsLost stats and recomputes Score.
    /// Idempotent for edited Done matches (removes previous results first).
    /// </summary>
    public async Task RecordAsync(TournamentMatch match, ICollection<TournamentTeamMatchResult> newResults)
    {
        var tournament = match.Tournament ?? await _db.Tournaments.FindAsync(match.TournamentId)
            ?? throw new InvalidOperationException("Tournament not found");
        var scoring = new ScoringSystemSelector().For(tournament);

        // Reload teams+players fresh.
        await _db.Entry(match).Collection(m => m.Teams!).LoadAsync();
        foreach (var team in match.Teams)
            await _db.Entry(team).Collection(t => t.Players!).LoadAsync();

        // Remove previous results if editing.
        var existing = await _db.TournamentTeamMatchResults.Where(r => r.MatchId == match.Id).ToListAsync();
        if (existing.Count > 0) _db.TournamentTeamMatchResults.RemoveRange(existing);

        // Reset per-player deltas for this match.
        foreach (var team in match.Teams)
            foreach (var p in team.Players)
                p.ScoreDiff = 0;

        // Attach new results.
        foreach (var r in newResults)
        {
            r.MatchId = match.Id;
            r.TournamentId = match.TournamentId;
            _db.TournamentTeamMatchResults.Add(r);
        }

        // Update aggregate stats.
        var resultByTeam = newResults.ToDictionary(r => r.TeamId);
        foreach (var team in match.Teams)
        {
            if (!resultByTeam.TryGetValue(team.Id, out var r)) continue;
            var won = r.GoalsWon > r.GoalsLost;
            foreach (var p in team.Players)
            {
                p.MatchCount += 1;
                if (won) p.WinCount += 1; else p.LoseCount += 1;
                p.PointsWon += r.GoalsWon;
                p.PointsLost += r.GoalsLost;
            }
        }

        // Apply scoring.
        scoring.Apply(tournament, match, newResults);

        match.State = MatchState.Done;
        await _db.SaveChangesAsync();
    }
}