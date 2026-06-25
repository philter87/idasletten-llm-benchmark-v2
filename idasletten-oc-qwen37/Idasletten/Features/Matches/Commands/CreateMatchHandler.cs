using Idasletten.Data;
using Idasletten.Features.Matches.Events;
using Idasletten.Features.Users.Commands;
using Idasletten.Models;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class CreateMatchHandler : IRequestHandler<CreateMatchCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public CreateMatchHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players)
                .ThenInclude(p => p.User)
            .Include(t => t.Teams)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        var match = new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            Order = request.Order,
            State = MatchState.Planned
        };

        var teamNumber = (await _db.TournamentTeams.CountAsync(t => t.TournamentId == request.TournamentId, cancellationToken)) + 1;

        foreach (var teamDto in request.TeamResults)
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                TournamentId = request.TournamentId,
                Name = $"Team {teamNumber}",
                Number = teamNumber
            };

            foreach (var initial in teamDto.PlayerInitials)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == initial, cancellationToken);
                if (user == null)
                {
                    var userId = await _mediator.Send(new CreateUserCommand(initial, null, null, null), cancellationToken);
                    user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
                }

                var player = tournament.Players.FirstOrDefault(p => p.UserId == user!.Id);
                if (player == null)
                {
                    player = new TournamentPlayer
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        TournamentId = request.TournamentId,
                        Score = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 1500,
                        Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0
                    };
                    _db.TournamentPlayers.Add(player);
                }

                team.Players.Add(player);
            }

            _db.TournamentTeams.Add(team);

            var result = new TournamentTeamMatchResult
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                TournamentId = request.TournamentId,
                TeamId = team.Id,
                GoalsWon = teamDto.GoalsWon,
                GoalsLost = request.TeamResults.Where(t => t != teamDto).Sum(t => t.GoalsWon)
            };

            match.TeamResults.Add(result);
            teamNumber++;
        }

        _db.TournamentMatches.Add(match);
        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new MatchCreated(match.Id), cancellationToken);

        return match.Id;
    }
}
