using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerCommand(Guid TournamentId, string Username, string? Name = null) : IRequest<Guid>;

public record AddPlayersFromTournamentCommand(Guid TournamentId, Guid SourceTournamentId, List<Guid> UserIds)
    : IRequest;

public class PlayerCommandHandlers(IdaslettenDbContext db, IMediator mediator)
    : IRequestHandler<AddPlayerCommand, Guid>,
      IRequestHandler<AddPlayersFromTournamentCommand>
{
    private readonly IdaslettenDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<Guid> Handle(AddPlayerCommand req, CancellationToken ct)
    {
        var t = await _db.Tournaments.FindAsync(req.TournamentId)
            ?? throw new InvalidOperationException("Tournament not found");
        if (t.MaxPlayerCount is int max)
        {
            var count = await _db.TournamentPlayers.CountAsync(p => p.TournamentId == req.TournamentId, ct);
            if (count >= max) throw new InvalidOperationException("MaxPlayerCount reached");
        }
        var userId = await _mediator.Send(new EnsureUserCommand(req.Username, req.Name), ct);
        var existing = await _db.TournamentPlayers.FirstOrDefaultAsync(
            p => p.TournamentId == req.TournamentId && p.UserId == userId, ct);
        if (existing is not null) return existing.Id;

        var scoring = new ScoringSystemSelector().For(t);
        var tp = new TournamentPlayer { UserId = userId, TournamentId = req.TournamentId };
        scoring.Initialise(tp);
        _db.TournamentPlayers.Add(tp);
        await _db.SaveChangesAsync(ct);
        await _mediator.Publish(new PlayerAdded(req.TournamentId, userId), ct);
        return tp.Id;
    }

    public async Task Handle(AddPlayersFromTournamentCommand req, CancellationToken ct)
    {
        foreach (var uid in req.UserIds)
        {
            var exists = await _db.TournamentPlayers.AnyAsync(
                p => p.TournamentId == req.TournamentId && p.UserId == uid, ct);
            if (exists) continue;
            var t = await _db.Tournaments.FindAsync(req.TournamentId)
                ?? throw new InvalidOperationException("Tournament not found");
            var scoring = new ScoringSystemSelector().For(t);
            var tp = new TournamentPlayer { UserId = uid, TournamentId = req.TournamentId };
            scoring.Initialise(tp);
            _db.TournamentPlayers.Add(tp);
            await _mediator.Publish(new PlayerAdded(req.TournamentId, uid), ct);
        }
        await _db.SaveChangesAsync(ct);
    }
}

public record PlayerAdded(Guid TournamentId, Guid UserId) : INotification;