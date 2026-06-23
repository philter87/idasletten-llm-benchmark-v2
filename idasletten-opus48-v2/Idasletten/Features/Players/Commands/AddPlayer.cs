using Idasletten.Data;
using Idasletten.Shared;
using Idasletten.Shared.Events;
using Idasletten.Shared.Graph;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

/// <summary>Adds a player (by initials) to a tournament, creating the user if new.</summary>
public record AddPlayerCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<Guid>;

public record PlayerAdded(Guid TournamentId, Guid UserId) : IDomainEvent;

public class AddPlayerHandler : IRequestHandler<AddPlayerCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IUserImageService _images;
    private readonly ScoreService _scores;
    private readonly IPublisher _publisher;

    public AddPlayerHandler(AppDbContext db, IUserImageService images, ScoreService scores, IPublisher publisher)
    {
        _db = db;
        _images = images;
        _scores = scores;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(AddPlayerCommand cmd, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FirstAsync(t => t.Id == cmd.TournamentId, ct);
        var user = await Provisioning.GetOrCreateUserAsync(_db, _images, cmd.Initials, cmd.Name, ct);
        await Provisioning.AddPlayerAsync(_db, _scores, tournament, user, ct);
        await _db.SaveChangesAsync(ct);

        await _publisher.Publish(new PlayerAdded(tournament.Id, user.Id), ct);
        return user.Id;
    }
}
