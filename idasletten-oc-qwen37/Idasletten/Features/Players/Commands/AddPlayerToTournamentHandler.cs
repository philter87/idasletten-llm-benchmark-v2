using Idasletten.Data;
using Idasletten.Features.Players.Events;
using Idasletten.Features.Users.Commands;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public class AddPlayerToTournamentHandler : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public AddPlayerToTournamentHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { request.TournamentId }, cancellationToken);
        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user == null)
        {
            var userId = await _mediator.Send(new CreateUserCommand(request.Username, request.Name, null, null), cancellationToken);
            user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        }

        var existingPlayer = await _db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == request.TournamentId && p.UserId == user!.Id, cancellationToken);

        if (existingPlayer != null)
            return existingPlayer.Id;

        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TournamentId = request.TournamentId,
            Score = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 1500,
            Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0
        };

        _db.TournamentPlayers.Add(player);
        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new PlayerAddedToTournament(player.Id, tournament.Id), cancellationToken);

        return player.Id;
    }
}
