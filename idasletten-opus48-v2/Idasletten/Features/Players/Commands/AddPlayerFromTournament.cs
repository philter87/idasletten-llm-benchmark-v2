using Idasletten.Data;
using Idasletten.Shared;
using Idasletten.Shared.Events;
using Idasletten.Shared.Graph;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

/// <summary>
/// Adds a single player from a previous (seed) tournament into the current one. Also records the
/// seed tournament on the current tournament the first time one is used.
/// </summary>
public record AddPlayerFromTournamentCommand(Guid TournamentId, Guid SeedTournamentId, Guid UserId) : IRequest<Guid>;

public class AddPlayerFromTournamentHandler : IRequestHandler<AddPlayerFromTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IUserImageService _images;
    private readonly ScoreService _scores;
    private readonly IPublisher _publisher;

    public AddPlayerFromTournamentHandler(AppDbContext db, IUserImageService images, ScoreService scores, IPublisher publisher)
    {
        _db = db;
        _images = images;
        _scores = scores;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(AddPlayerFromTournamentCommand cmd, CancellationToken ct)
    {
        var tournament = await _db.Tournaments.FirstAsync(t => t.Id == cmd.TournamentId, ct);
        if (tournament.SeedTournamentId is null && tournament.ParentTournamentId is null)
            tournament.SeedTournamentId = cmd.SeedTournamentId;

        var user = await _db.Users.FirstAsync(u => u.Id == cmd.UserId, ct);
        await Provisioning.AddPlayerAsync(_db, _scores, tournament, user, ct);
        await _db.SaveChangesAsync(ct);

        await _publisher.Publish(new PlayerAdded(tournament.Id, user.Id), ct);
        return user.Id;
    }
}
