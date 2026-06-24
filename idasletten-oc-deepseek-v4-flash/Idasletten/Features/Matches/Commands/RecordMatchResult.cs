using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using Idasletten.Shared.Events;
using Idasletten.Features.ScoreSystems;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record RecordMatchResultCommand(
    Guid MatchId,
    int GoalsWonTeam1,
    int GoalsWonTeam2
) : IRequest;

public class RecordMatchResultHandler : IRequestHandler<RecordMatchResultCommand>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;
    private readonly IEnumerable<IScoringSystem> _scoringSystems;

    public RecordMatchResultHandler(AppDbContext db, IPublisher publisher, IEnumerable<IScoringSystem> scoringSystems)
    {
        _db = db;
        _publisher = publisher;
        _scoringSystems = scoringSystems;
    }

    public async Task Handle(RecordMatchResultCommand request, CancellationToken cancellationToken)
    {
        var match = await _db.TournamentMatches
            .Include(m => m.TeamEntries)
            .Include(m => m.Results)
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match == null)
            throw new InvalidOperationException("Match not found.");

        var teamEntries = match.TeamEntries.OrderBy(te => te.Team.Number).ToList();
        if (teamEntries.Count < 2)
            throw new InvalidOperationException("Match must have at least two teams.");

        var team1 = teamEntries[0].Team;
        var team2 = teamEntries[1].Team;

        // Clear existing results
        _db.TournamentTeamMatchResults.RemoveRange(match.Results);

        var result1 = new TournamentTeamMatchResult
        {
            MatchId = request.MatchId,
            TournamentId = match.TournamentId,
            TeamId = team1.Id,
            GoalsWon = request.GoalsWonTeam1,
            GoalsLost = request.GoalsWonTeam2
        };
        var result2 = new TournamentTeamMatchResult
        {
            MatchId = request.MatchId,
            TournamentId = match.TournamentId,
            TeamId = team2.Id,
            GoalsWon = request.GoalsWonTeam2,
            GoalsLost = request.GoalsWonTeam1
        };
        _db.TournamentTeamMatchResults.AddRange(result1, result2);

        match.State = MatchState.Done;

        // Update player stats
        var team1Players = await _db.TournamentTeamPlayers
            .Where(tp => tp.TeamId == team1.Id)
            .Select(tp => tp.Player)
            .ToListAsync(cancellationToken);
        var team2Players = await _db.TournamentTeamPlayers
            .Where(tp => tp.TeamId == team2.Id)
            .Select(tp => tp.Player)
            .ToListAsync(cancellationToken);

        var allPlayers = team1Players.Concat(team2Players).ToList();

        var scoringSystem = _scoringSystems.FirstOrDefault(s => s.Type == match.Tournament.ScoreSystem);
        if (scoringSystem != null)
        {
            scoringSystem.Calculate(team1Players.Select(p => p.UserId).ToList(),
                team2Players.Select(p => p.UserId).ToList(),
                request.GoalsWonTeam1, request.GoalsWonTeam2, allPlayers);
        }

        foreach (var player in team1Players)
        {
            var oldScore = player.Score;
            player.MatchCount++;
            player.PointsWon += request.GoalsWonTeam1;
            player.PointsLost += request.GoalsWonTeam2;
            if (request.GoalsWonTeam1 > request.GoalsWonTeam2)
            {
                player.WinCount++;
                if (match.Tournament.ScoreSystem == ScoreSystem.WinCount)
                    player.Score = player.WinCount;
            }
            else
            {
                player.LoseCount++;
                if (match.Tournament.ScoreSystem == ScoreSystem.Lives)
                    player.Lives--;
            }
            player.ScoreDiff = player.Score - oldScore;
        }

        foreach (var player in team2Players)
        {
            var oldScore = player.Score;
            player.MatchCount++;
            player.PointsWon += request.GoalsWonTeam2;
            player.PointsLost += request.GoalsWonTeam1;
            if (request.GoalsWonTeam2 > request.GoalsWonTeam1)
            {
                player.WinCount++;
                if (match.Tournament.ScoreSystem == ScoreSystem.WinCount)
                    player.Score = player.WinCount;
            }
            else
            {
                player.LoseCount++;
                if (match.Tournament.ScoreSystem == ScoreSystem.Lives)
                    player.Lives--;
            }
            player.ScoreDiff = player.Score - oldScore;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new MatchResultRecorded(request.MatchId, match.TournamentId), cancellationToken);
    }
}
