using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerToTournamentCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<Guid>;

public record PlayerAddedToTournament(Guid PlayerId, Guid TournamentId) : INotification;

public class AddPlayerToTournamentHandler : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public AddPlayerToTournamentHandler(AppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(AddPlayerToTournamentCommand command, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FindAsync([command.TournamentId], ct)
            ?? throw new InvalidOperationException("Tournament not found");

        var user = await _mediator.Send(new CreateUserCommand(command.Initials, command.Name), ct);

        var existing = await _db.TournamentPlayers
            .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == tournament.Id, ct);
        if (existing != null) return existing.Id;

        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TournamentId = tournament.Id,
            Score = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 1000,
            Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0
        };

        _db.TournamentPlayers.Add(player);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new PlayerAddedToTournament(player.Id, tournament.Id), ct);
        return player.Id;
    }
}
