using Idasletten.Data;
using Idasletten.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Recomputes every player's standing for a tournament by replaying its completed matches in
/// order. Used after recording a new result and after editing an existing one, so the two paths
/// always agree.
/// </summary>
public class ScoreService
{
    private readonly AppDbContext _db;
    private readonly IReadOnlyDictionary<ScoreSystem, IScoreCalculator> _calculators;

    public ScoreService(AppDbContext db, IEnumerable<IScoreCalculator> calculators)
    {
        _db = db;
        _calculators = calculators.ToDictionary(c => c.System);
    }

    public IScoreCalculator CalculatorFor(ScoreSystem system) => _calculators[system];

    public async Task RecalculateAsync(Guid tournamentId, CancellationToken ct = default)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);
        if (tournament is null) return;

        var matches = await _db.TournamentMatches
            .Where(m => m.TournamentId == tournamentId && m.State == MatchState.Done)
            .OrderBy(m => m.Order)
            .Include(m => m.Results).ThenInclude(r => r.Team).ThenInclude(t => t.Players)
            .ToListAsync(ct);

        var calculator = _calculators[tournament.ScoreSystem];

        // Reset all players to a clean slate.
        foreach (var p in tournament.Players)
        {
            p.Score = calculator.InitialScore;
            p.ScoreDiff = 0;
            p.WinCount = 0;
            p.LoseCount = 0;
            p.MatchCount = 0;
            p.PointsWon = 0;
            p.PointsLost = 0;
            p.Lives = 3;
        }

        var state = new Dictionary<string, object>();

        foreach (var match in matches)
        {
            int maxGoals = match.Results.Count == 0 ? 0 : match.Results.Max(r => r.GoalsWon);
            int winners = match.Results.Count(r => r.GoalsWon == maxGoals);

            var teamResults = match.Results
                .Select(r => new TeamResult(r.Team, r.Team.Players, r.GoalsWon, r.GoalsLost)
                {
                    IsWinner = r.GoalsWon == maxGoals && winners == 1,
                    IsTie = winners > 1 && r.GoalsWon == maxGoals
                })
                .ToList();

            // Aggregate counters first (some calculators read WinCount).
            foreach (var tr in teamResults)
            {
                foreach (var player in tr.Players)
                {
                    player.MatchCount++;
                    player.PointsWon += tr.GoalsWon;
                    player.PointsLost += tr.GoalsLost;
                    if (tr.IsWinner) player.WinCount++;
                    else if (!tr.IsTie) player.LoseCount++;
                }
            }

            calculator.ApplyMatch(tournament, teamResults, state);
        }

        await _db.SaveChangesAsync(ct);
    }
}
