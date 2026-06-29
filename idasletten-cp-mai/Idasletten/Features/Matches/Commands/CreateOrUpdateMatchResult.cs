using Idasletten.Features.Players.Commands;
using Idasletten.Features.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public record TeamResultInput(
    Guid? TeamId,
    int Number,
    List<string> Initials,
    int GoalsWon);

public record CreateOrUpdateMatchResultCommand(
    Guid TournamentId,
    Guid MatchId,
    List<TeamResultInput> Teams) : IRequest<Guid>;

public class MatchResultRecorded : INotification
{
    public Guid TournamentId { get; set; }
    public Guid MatchId { get; set; }
}

public class CreateOrUpdateMatchResultHandler : IRequestHandler<CreateOrUpdateMatchResultCommand, Guid>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IMediator _mediator;
    private readonly IPublisher _publisher;
    private readonly ITournamentRecalculator _recalculator;

    public CreateOrUpdateMatchResultHandler(Shared.Data.ApplicationDbContext db, IMediator mediator, IPublisher publisher, ITournamentRecalculator recalculator)
    {
        _db = db;
        _mediator = mediator;
        _publisher = publisher;
        _recalculator = recalculator;
    }

    public async Task<Guid> Handle(CreateOrUpdateMatchResultCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { request.TournamentId }, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        var match = await _db.TournamentMatches
            .Include(m => m.Teams)
                .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        bool isNew = match == null;
        if (match == null)
        {
            var maxOrder = await _db.TournamentMatches
                .Where(m => m.TournamentId == request.TournamentId)
                .MaxAsync(m => (int?)m.Order, cancellationToken) ?? 0;

            match = new TournamentMatch
            {
                Id = request.MatchId,
                TournamentId = request.TournamentId,
                Order = maxOrder + 1,
                State = MatchState.Planned
            };
            _db.TournamentMatches.Add(match);
        }
        else
        {
            // Remove old teams to rebuild
            _db.TournamentTeams.RemoveRange(match.Teams);
            match.Teams.Clear();
        }

        // Ensure player tournament entries and resolve user ids
        var userIdByInitials = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var teamInput in request.Teams)
        {
            foreach (var initials in teamInput.Initials)
            {
                if (!userIdByInitials.ContainsKey(initials))
                {
                    var playerId = await _mediator.Send(new AddPlayerToTournamentCommand(request.TournamentId, initials), cancellationToken);
                    var player = await _db.TournamentPlayers.FindAsync(new object[] { playerId }, cancellationToken)
                        ?? throw new InvalidOperationException("Player could not be created");
                    userIdByInitials[initials] = player.UserId;
                }
            }
        }

        // Build teams
        int teamNumber = 1;
        foreach (var teamInput in request.Teams)
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                Name = $"Team {teamNumber}",
                Number = teamNumber++,
                GoalsWon = teamInput.GoalsWon
            };

            foreach (var initials in teamInput.Initials)
            {
                var userId = userIdByInitials[initials];
                var player = await _db.TournamentPlayers
                    .FirstAsync(p => p.TournamentId == request.TournamentId && p.UserId == userId, cancellationToken);
                team.Members.Add(player);
            }

            _db.TournamentTeams.Add(team);
            match.Teams.Add(team);
        }

        // Derive goals lost for each team
        var teamList = match.Teams.ToList();
        foreach (var team in teamList)
        {
            team.GoalsLost = teamList.Where(t => t.Id != team.Id).Sum(t => t.GoalsWon);
        }

        match.State = MatchState.Done;
        match.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _recalculator.RecalculateAsync(request.TournamentId, cancellationToken);
        await _publisher.Publish(new MatchResultRecorded { TournamentId = request.TournamentId, MatchId = match.Id }, cancellationToken);
        return match.Id;
    }
}
