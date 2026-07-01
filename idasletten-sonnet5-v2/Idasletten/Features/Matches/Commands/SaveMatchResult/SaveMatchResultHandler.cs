using Idasletten.Data;
using Idasletten.Features.Players.Commands.AddPlayerToTournament;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.SaveMatchResult;

public class SaveMatchResultHandler(IdaslettenDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<SaveMatchResultCommand>
{
    public async Task Handle(SaveMatchResultCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .Include(t => t.Matches).ThenInclude(m => m.Teams).ThenInclude(team => team.TeamPlayers)
            .Include(t => t.Matches).ThenInclude(m => m.Results)
            .FirstAsync(t => t.Id == request.TournamentId, cancellationToken);

        var existingMatch = tournament.Matches.FirstOrDefault(m => m.Id == request.MatchId);
        if (existingMatch is not null && existingMatch.State == MatchState.Done && !request.IsEditAuthorized)
        {
            throw new UnauthorizedAccessException("Editing a completed match requires logging in.");
        }

        // Ensure every referenced player exists in the tournament (auto-creating Users/TournamentPlayers by initials).
        var teamPlayerIds = new List<List<Guid>>();
        foreach (var teamInput in request.Teams)
        {
            var ids = new List<Guid>();
            foreach (var initials in teamInput.PlayerInitials)
            {
                ids.Add(await sender.Send(new AddPlayerToTournamentCommand(tournament.Id, initials), cancellationToken));
            }
            teamPlayerIds.Add(ids);
        }

        var allPlayerIds = teamPlayerIds.SelectMany(ids => ids).ToHashSet();
        var playersById = await db.TournamentPlayers
            .Where(p => allPlayerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        if (existingMatch is not null)
        {
            // Team membership may have changed on edit; simplest correct approach is to
            // drop and recreate the teams/results rather than diff them.
            db.TournamentTeamMatchResults.RemoveRange(existingMatch.Results);
            db.TournamentTeams.RemoveRange(existingMatch.Teams);
            existingMatch.Results.Clear();
            existingMatch.Teams.Clear();
        }

        var match = existingMatch ?? new TournamentMatch
        {
            Id = request.MatchId,
            TournamentId = tournament.Id,
            Order = tournament.Matches.Count == 0 ? 1 : tournament.Matches.Max(m => m.Order) + 1,
        };
        match.State = MatchState.Done;

        if (existingMatch is null)
        {
            tournament.Matches.Add(match);
            db.TournamentMatches.Add(match);
        }

        for (var i = 0; i < request.Teams.Count; i++)
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                Number = i + 1,
                Name = $"Team {i + 1}",
            };

            foreach (var playerId in teamPlayerIds[i])
            {
                team.TeamPlayers.Add(new TournamentTeamPlayer
                {
                    TeamId = team.Id,
                    TournamentPlayerId = playerId,
                    TournamentPlayer = playersById[playerId],
                });
            }
            match.Teams.Add(team);
            db.TournamentTeams.Add(team);

            var goalsLost = request.Teams.Where((_, idx) => idx != i).Sum(t => t.GoalsWon);
            var result = new TournamentTeamMatchResult
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TeamId = team.Id,
                GoalsWon = request.Teams[i].GoalsWon,
                GoalsLost = goalsLost,
            };
            match.Results.Add(result);
            db.TournamentTeamMatchResults.Add(result);
        }

        TournamentScoreRecalculator.Recalculate(tournament);

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new MatchResultSaved(tournament.Id, match.Id), cancellationToken);
    }
}
