using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Tournaments.Events;
using Idasletten.Models;
using Idasletten.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands.CreateTournament;

public sealed record CreateTournamentCommand(
    string Name,
    int? MaxPlayerCount,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    bool IsPublic,
    Guid? ParentTournamentId,
    IReadOnlyList<Guid> CarryOverPlayerUserIds) : IRequest<Guid>;

public sealed class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly ScoringEngine _scoring;
    private readonly IPublisher _publisher;

    public CreateTournamentCommandHandler(AppDbContext db, ScoringEngine scoring, IPublisher publisher)
    {
        _db = db;
        _scoring = scoring;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new FeatureException("Tournament name is required.");
        if (request.TeamSize < 1 || request.TeamSize > 8)
            throw new FeatureException("Team size must be between 1 and 8.");
        if (request.PointsToWin < 1 || request.PointsToWin > 99)
            throw new FeatureException("Points to win must be between 1 and 99.");
        if (request.MaxPlayerCount is int max && max < 2 * request.TeamSize)
            throw new FeatureException($"Max player count must be at least {2 * request.TeamSize} (two teams).");

        Tournament? parent = null;
        if (request.ParentTournamentId is Guid parentId)
        {
            parent = await _db.Tournaments.FirstOrDefaultAsync(t => t.Id == parentId, cancellationToken)
                ?? throw new FeatureException("Parent tournament not found.");
            if (parent.IsArchived)
                throw new FeatureException("Cannot start a new round from an archived tournament.");
            if (parent.ParentTournamentId is not null)
                throw new FeatureException("Only the first round of a tournament chain can have rounds started from it.");
        }

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            ParentTournamentId = parent?.Id,
            RoundNumber = parent is null ? 1 : (parent.RoundNumber ?? 1) + 1
        };

        _db.Tournaments.Add(tournament);

        if (parent is not null)
        {
            var carryOver = request.CarryOverPlayerUserIds ?? Array.Empty<Guid>();
            if (carryOver.Count > 0)
            {
                var parentPlayerUserIds = await _db.TournamentPlayers
                    .Where(p => p.TournamentId == parent.Id)
                    .Select(p => p.UserId)
                    .ToHashSetAsync(cancellationToken);
                foreach (var userId in carryOver.Distinct())
                {
                    if (!parentPlayerUserIds.Contains(userId))
                        throw new FeatureException("A carried-over player is not part of the parent tournament.");
                }

                foreach (var userId in carryOver.Distinct())
                {
                    _db.TournamentPlayers.Add(new TournamentPlayer { TournamentId = tournament.Id, UserId = userId });
                }
            }
        }
        else if (request.CarryOverPlayerUserIds is { Count: > 0 })
        {
            throw new FeatureException("Carrying over players only applies when creating a round from a parent tournament.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        foreach (var p in tournament.Players.ToList())
            _scoring.InitializePlayer(p);
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TournamentCreated(tournament.Id), cancellationToken);
        return tournament.Id;
    }
}
