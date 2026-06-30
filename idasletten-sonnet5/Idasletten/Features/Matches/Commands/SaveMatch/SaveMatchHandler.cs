using Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.SaveMatch;

public class SaveMatchHandler(IdaslettenDbContext db, ISender sender, IPublisher publisher, ScoreRecalculator recalculator)
    : IRequestHandler<SaveMatchCommand>
{
    public async Task Handle(SaveMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches.FirstAsync(m => m.Id == request.MatchId, cancellationToken);

        var existingTeams = await db.TournamentTeams.Where(t => t.MatchId == match.Id).ToListAsync(cancellationToken);
        var existingTeamIds = existingTeams.Select(t => t.Id).ToList();
        db.TournamentTeamPlayers.RemoveRange(
            db.TournamentTeamPlayers.Where(tp => existingTeamIds.Contains(tp.TeamId)));
        db.TournamentTeamMatchResults.RemoveRange(
            db.TournamentTeamMatchResults.Where(r => r.MatchId == match.Id));
        db.TournamentTeams.RemoveRange(existingTeams);
        await db.SaveChangesAsync(cancellationToken);

        var newTeams = new List<TournamentTeam>();
        for (var i = 0; i < request.Teams.Count; i++)
        {
            var teamInput = request.Teams[i];
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                Number = i + 1,
                Name = $"Team {i + 1}"
            };

            foreach (var initials in teamInput.Initials)
            {
                var playerId = await sender.Send(
                    new AddPlayerToTournamentCommand(request.TournamentId, initials), cancellationToken);
                team.Players.Add(new TournamentTeamPlayer { TeamId = team.Id, TournamentPlayerId = playerId });
            }

            newTeams.Add(team);
        }

        db.TournamentTeams.AddRange(newTeams);

        if (request.RecordResult)
        {
            for (var i = 0; i < newTeams.Count; i++)
            {
                var ownScore = request.Teams[i].Score;
                var opponentScores = request.Teams.Where((_, j) => j != i).Select(t => t.Score).ToList();
                var worstOpponentScore = opponentScores.Count > 0 ? opponentScores.Max() : 0;

                db.TournamentTeamMatchResults.Add(new TournamentTeamMatchResult
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    TournamentId = request.TournamentId,
                    TeamId = newTeams[i].Id,
                    GoalsWon = ownScore,
                    GoalsLost = worstOpponentScore
                });
            }

            match.State = MatchState.Done;
        }
        else
        {
            match.State = MatchState.Planned;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (request.RecordResult)
        {
            await recalculator.RecalculateAsync(request.TournamentId, cancellationToken);
            await publisher.Publish(new MatchResultRecorded(match.Id, request.TournamentId), cancellationToken);
        }
        else
        {
            await publisher.Publish(new MatchPlanned(match.Id, request.TournamentId), cancellationToken);
        }
    }
}
