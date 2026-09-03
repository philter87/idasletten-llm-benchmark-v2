using Idasletten.Data;
using Idasletten.Models;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Scoring;

/// <summary>
/// Facade over the four score systems. The authoritative state of a tournament's
/// scoreboard is always produced by replaying its finished matches in order
/// (<see cref="RecalculateTournament"/>), so editing a finished match simply
/// rewrites the result and replays — no manual undo bookkeeping.
/// </summary>
public sealed class ScoringEngine
{
    private readonly IReadOnlyDictionary<ScoreSystem, IScoringEngine> _engines;

    public ScoringEngine(IEnumerable<IScoringEngine> engines)
    {
        _engines = engines.ToDictionary(e => e.System);
    }

    public IScoringEngine Get(ScoreSystem system) => _engines[system];

    /// <summary>Initial score-state for a new player (call after adding to a tournament).</summary>
    public void InitializePlayer(TournamentPlayer player)
    {
        Get(player.Tournament.ScoreSystem).Initialize(player);
    }

    /// <summary>
    /// Reset every player in the tournament to the initial state and replay all
    /// finished matches in Order. Called after recording or editing a result.
    /// </summary>
    public async Task RecalculateTournamentAsync(AppDbContext db, Tournament tournament, CancellationToken ct = default)
    {
        var players = await db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id)
            .ToListAsync(ct);

        var engine = Get(tournament.ScoreSystem);
        foreach (var p in players)
        {
            engine.Initialize(p);
            p.WinCount = 0;
            p.MatchCount = 0;
            p.LoseCount = 0;
            p.PointsWon = 0;
            p.PointsLost = 0;
            p.ScoreDiff = 0;
        }

        var matches = await db.TournamentMatches
            .Include(m => m.Results)
            .Where(m => m.TournamentId == tournament.Id && m.State == MatchState.Done)
            .OrderBy(m => m.Order)
            .ToListAsync(ct);

        foreach (var match in matches)
        {
            var teams = await LoadTeamsAsync(db, match, ct);

            // Snapshot the pre-match score state so every team's delta is
            // computed against the state before the match (order independent).
            var preMatch = players.ToDictionary(p => p.Id, p => (p.Score, p.TrueSkillSigma, p.Lives));
            var finals = new Dictionary<Guid, (double Score, double Sigma, int Lives)>();
            foreach (var team in teams)
            {
                foreach (var p in players)
                {
                    p.Score = preMatch[p.Id].Score;
                    p.TrueSkillSigma = preMatch[p.Id].Item2;
                    p.Lives = preMatch[p.Id].Item3;
                }

                // Common counters for every system.
                int opponents = team.OpponentsGoals(teams);
                foreach (var p in team.Players)
                {
                    p.MatchCount++;
                    p.PointsWon += team.Goals;
                    p.PointsLost += opponents;
                    if (MatchOutcomes.Won(team, teams)) p.WinCount++;
                    if (MatchOutcomes.Lost(team, teams)) p.LoseCount++;
                }
                engine.Apply(team.Players.ToArray(), team.Goals, teams);

                foreach (var p in team.Players)
                    finals[p.Id] = (p.Score, p.TrueSkillSigma, p.Lives);
            }
            foreach (var p in players)
                if (finals.TryGetValue(p.Id, out var f))
                {
                    p.Score = f.Score;
                    p.TrueSkillSigma = f.Sigma;
                    p.Lives = f.Lives;
                }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<TeamResult>> LoadTeamsAsync(AppDbContext db, TournamentMatch match, CancellationToken ct)
    {
        var results = match.Results;
        var teams = new List<TeamResult>();
        foreach (var r in results)
        {
            var playerIds = await db.TeamPlayers
                .Where(tp => tp.TeamId == r.TeamId)
                .Select(tp => tp.TournamentPlayerId)
                .ToListAsync(ct);
            var players = await db.TournamentPlayers.Where(p => playerIds.Contains(p.Id)).ToListAsync(ct);
            teams.Add(new TeamResult { Players = players, Goals = r.GoalsWon });
        }
        return teams;
    }
}
