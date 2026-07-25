using Idasletten.Features.Players;
using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

/// <summary>
/// Creates a tournament. When <see cref="ParentTournamentId"/> is set the new tournament is the next
/// round of that tournament: the best <see cref="AdvancingPlayerCount"/> players are carried over with
/// their scores reset, and the round number is incremented.
/// </summary>
public record CreateTournament(
    string Name,
    int TeamSize = 2,
    int PointsToWin = 5,
    ScoreSystem ScoreSystem = ScoreSystem.Elo,
    int? MaxPlayerCount = null,
    bool IsPublic = true,
    Guid? SeedTournamentId = null,
    Guid? ParentTournamentId = null,
    int? AdvancingPlayerCount = null) : IRequest<Guid>;

public class CreateTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<CreateTournament, Guid>
{
    public async Task<Guid> Handle(CreateTournament request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A tournament needs a name.", nameof(request));
        }

        Tournament? parent = null;
        if (request.ParentTournamentId is { } parentId)
        {
            parent = await db.Tournaments
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == parentId, cancellationToken)
                ?? throw new ArgumentException("The parent tournament does not exist.", nameof(request));
        }

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TeamSize = Math.Max(1, request.TeamSize),
            PointsToWin = Math.Max(1, request.PointsToWin),
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount is > 0 ? request.MaxPlayerCount : null,
            IsPublic = request.IsPublic,
            ParentTournamentId = parent?.Id,
            RoundNumber = parent is null ? 1 : (parent.RoundNumber ?? 1) + 1,
            // A tournament may only be seeded when it has no parent - a round inherits its players.
            SeedTournamentId = parent is null ? request.SeedTournamentId : null,
        };

        db.Tournaments.Add(tournament);

        if (parent is not null)
        {
            foreach (var player in AdvancingPlayers(parent, request.AdvancingPlayerCount))
            {
                var carriedOver = new TournamentPlayer
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournament.Id,
                    UserId = player.UserId,
                };

                ScoreEngine.Reset(tournament, carriedOver);
                db.TournamentPlayers.Add(carriedOver);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new TournamentCreated(
                tournament.Id, tournament.Name, tournament.ScoreSystem,
                tournament.ParentTournamentId, tournament.RoundNumber),
            cancellationToken);

        return tournament.Id;
    }

    private static IEnumerable<TournamentPlayer> AdvancingPlayers(Tournament parent, int? advancingCount)
    {
        var ranked = ScoreEngine.Rank(parent.Players);
        return advancingCount is > 0 ? ranked.Take(advancingCount.Value) : ranked;
    }
}
