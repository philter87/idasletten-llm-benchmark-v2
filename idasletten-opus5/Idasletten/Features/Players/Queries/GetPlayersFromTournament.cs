using Idasletten.Features.Scoring;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

/// <summary>
/// The players of a previous tournament, ordered by their score there, flagged with whether they are
/// already part of the tournament we are adding players to.
/// </summary>
public record GetPlayersFromTournament(Guid SourceTournamentId, Guid TargetTournamentId)
    : IRequest<IReadOnlyList<SeedPlayerRow>>;

public record SeedPlayerRow(
    Guid UserId,
    string Initials,
    string DisplayName,
    double Score,
    int Rank,
    bool IsAlreadyAdded);

public class GetPlayersFromTournamentHandler(AppDbContext db)
    : IRequestHandler<GetPlayersFromTournament, IReadOnlyList<SeedPlayerRow>>
{
    public async Task<IReadOnlyList<SeedPlayerRow>> Handle(
        GetPlayersFromTournament request, CancellationToken cancellationToken)
    {
        var source = await db.TournamentPlayers
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.TournamentId == request.SourceTournamentId)
            .ToListAsync(cancellationToken);

        var alreadyAdded = await db.TournamentPlayers
            .AsNoTracking()
            .Where(p => p.TournamentId == request.TargetTournamentId)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        return ScoreEngine.Rank(source)
            .Select((player, index) => new SeedPlayerRow(
                player.UserId,
                player.User.Initials,
                player.User.DisplayName,
                player.Score,
                index + 1,
                alreadyAdded.Contains(player.UserId)))
            .ToList();
    }
}
