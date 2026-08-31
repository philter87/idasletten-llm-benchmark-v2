using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Players.Events;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users.Commands.FindOrCreateUser;
using Idasletten.Models;
using Idasletten.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands.AddPlayer;

public sealed record AddPlayerCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<PlayerRowDto>;

public sealed class AddPlayerCommandHandler : IRequestHandler<AddPlayerCommand, PlayerRowDto>
{
    private readonly AppDbContext _db;
    private readonly ScoringEngine _scoring;
    private readonly IMediator _mediator;
    private readonly IPublisher _publisher;

    public AddPlayerCommandHandler(AppDbContext db, ScoringEngine scoring, IMediator mediator, IPublisher publisher)
    {
        _db = db;
        _scoring = scoring;
        _mediator = mediator;
        _publisher = publisher;
    }

    public async Task<PlayerRowDto> Handle(AddPlayerCommand request, CancellationToken cancellationToken)
    {
        var tournament = await GetActiveTournamentAsync(_db, request.TournamentId, cancellationToken);

        var userId = await _mediator.Send(new FindOrCreateUserCommand(request.Initials, request.Name), cancellationToken);
        return await AddUserAsync(_db, _scoring, tournament, userId, _publisher, cancellationToken);
    }

    public static async Task<Tournament> GetActiveTournamentAsync(AppDbContext db, Guid tournamentId, CancellationToken ct)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, ct)
            ?? throw new FeatureException("Tournament not found.");
        if (t.IsArchived)
            throw new FeatureException("This tournament is archived; players can no longer be added.");
        return t;
    }

    public static async Task<PlayerRowDto> AddUserAsync(AppDbContext db, ScoringEngine scoring, Tournament tournament, Guid userId, IPublisher publisher, CancellationToken ct)
    {
        var playerCount = await db.TournamentPlayers.CountAsync(p => p.TournamentId == tournament.Id, ct);
        if (tournament.MaxPlayerCount is int max && playerCount >= max)
            throw new FeatureException($"This tournament is full (max {max} players).");

        var existing = await db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == tournament.Id && p.UserId == userId, ct);
        if (existing is not null)
        {
            var username = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstAsync(ct);
            throw new FeatureException($"{username} is already in this tournament.");
        }

        var player = new TournamentPlayer { TournamentId = tournament.Id, UserId = userId };
        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(ct);
        scoring.InitializePlayer(player);
        await db.SaveChangesAsync(ct);
        await publisher.Publish(new PlayerAdded(tournament.Id, player.Id), ct);

        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        return new PlayerRowDto(player.Id, player.UserId, user.Username, user.Name, user.Email, user.ImageUrl,
            player.Score, player.ScoreDiff, player.WinCount, player.LoseCount, player.MatchCount,
            player.PointsWon, player.PointsLost, player.Lives);
    }
}
