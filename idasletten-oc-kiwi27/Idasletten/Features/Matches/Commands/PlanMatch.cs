using Idasletten.Features.Players.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record PlanMatchCommand(
    Guid TournamentId,
    List<List<string>> TeamInitials) : IRequest<Guid>;

public class PlanMatchHandler : IRequestHandler<PlanMatchCommand, Guid>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IMediator _mediator;

    public PlanMatchHandler(Shared.Data.ApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(PlanMatchCommand request, CancellationToken cancellationToken)
    {
        var maxOrder = await _db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .MaxAsync(m => (int?)m.Order, cancellationToken) ?? 0;

        var match = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            Order = maxOrder + 1,
            State = MatchState.Planned
        };
        _db.TournamentMatches.Add(match);

        int teamNumber = 1;
        foreach (var initialsList in request.TeamInitials)
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                Name = $"Team {teamNumber}",
                Number = teamNumber++
            };

            foreach (var initials in initialsList)
            {
                var playerId = await _mediator.Send(new AddPlayerToTournamentCommand(request.TournamentId, initials), cancellationToken);
                var player = await _db.TournamentPlayers.FindAsync(new object[] { playerId }, cancellationToken)
                    ?? throw new InvalidOperationException("Player could not be created");
                team.Members.Add(player);
            }

            _db.TournamentTeams.Add(team);
            match.Teams.Add(team);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return match.Id;
    }
}
