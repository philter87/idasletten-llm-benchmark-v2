using Idasletten.Data;
using Idasletten.Features.Players.Commands.AddPlayerToTournament;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.AddPlannedMatch;

public class AddPlannedMatchHandler(IdaslettenDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<AddPlannedMatchCommand, Guid>
{
    public async Task<Guid> Handle(AddPlannedMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Matches)
            .FirstAsync(t => t.Id == request.TournamentId, cancellationToken);

        var match = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Order = tournament.Matches.Count == 0 ? 1 : tournament.Matches.Max(m => m.Order) + 1,
            State = MatchState.Planned,
        };
        db.TournamentMatches.Add(match);

        for (var i = 0; i < request.Teams.Count; i++)
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                Number = i + 1,
                Name = $"Team {i + 1}",
            };

            foreach (var initials in request.Teams[i])
            {
                var playerId = await sender.Send(new AddPlayerToTournamentCommand(tournament.Id, initials), cancellationToken);
                team.TeamPlayers.Add(new TournamentTeamPlayer { TeamId = team.Id, TournamentPlayerId = playerId });
            }

            db.TournamentTeams.Add(team);
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new MatchPlanned(tournament.Id, match.Id), cancellationToken);

        return match.Id;
    }
}
