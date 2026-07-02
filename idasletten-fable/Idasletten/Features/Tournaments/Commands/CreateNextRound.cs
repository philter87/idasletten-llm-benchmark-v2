using Idasletten.Features.Scoring;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

/// <summary>
/// Creates the next round of a tournament: a child tournament linked via
/// ParentTournamentId. The top players carry over with their scores reset.
/// </summary>
public record CreateNextRoundCommand(Guid ParentTournamentId, int? TopPlayerCount = null) : IRequest<Tournament>;

public record NextRoundCreated(Guid ParentTournamentId, Guid ChildTournamentId, int RoundNumber) : INotification;

public class CreateNextRoundHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<CreateNextRoundCommand, Tournament>
{
    public async Task<Tournament> Handle(CreateNextRoundCommand request, CancellationToken ct)
    {
        var parent = await db.Tournaments
            .Include(t => t.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == request.ParentTournamentId, ct)
            ?? throw new InvalidOperationException($"Tournament {request.ParentTournamentId} not found.");

        var roundNumber = (parent.RoundNumber ?? 1) + 1;
        var suffixIndex = parent.Name.LastIndexOf(" — Round", StringComparison.Ordinal);
        var baseName = suffixIndex > 0 ? parent.Name[..suffixIndex] : parent.Name;

        var child = new Tournament
        {
            Name = $"{baseName} — Round {roundNumber}",
            TeamSize = parent.TeamSize,
            PointsToWin = parent.PointsToWin,
            ScoreSystem = parent.ScoreSystem,
            MaxPlayerCount = parent.MaxPlayerCount,
            IsPublic = parent.IsPublic,
            ParentTournamentId = parent.Id,
            RoundNumber = roundNumber
        };
        db.Tournaments.Add(child);

        var carryOver = parent.Players
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .Take(request.TopPlayerCount ?? parent.Players.Count);

        foreach (var parentPlayer in carryOver)
        {
            var player = new TournamentPlayer
            {
                TournamentId = child.Id,
                UserId = parentPlayer.UserId
            };
            ScoringEngine.ResetPlayer(player, child.ScoreSystem);
            db.TournamentPlayers.Add(player);
        }

        await db.SaveChangesAsync(ct);
        await publisher.Publish(new NextRoundCreated(parent.Id, child.Id, roundNumber), ct);
        return child;
    }
}
