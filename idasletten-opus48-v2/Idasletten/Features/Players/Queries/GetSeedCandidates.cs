using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public record SeedCandidate(Guid UserId, string Initials, string Name, double Score, bool AlreadyAdded);

/// <summary>Players of a seed tournament, ranked, flagged if already in the current tournament.</summary>
public record GetSeedCandidatesQuery(Guid CurrentTournamentId, Guid SeedTournamentId) : IRequest<List<SeedCandidate>>;

public class GetSeedCandidatesHandler : IRequestHandler<GetSeedCandidatesQuery, List<SeedCandidate>>
{
    private readonly AppDbContext _db;
    public GetSeedCandidatesHandler(AppDbContext db) => _db = db;

    public async Task<List<SeedCandidate>> Handle(GetSeedCandidatesQuery q, CancellationToken ct)
    {
        var alreadyIn = await _db.TournamentPlayers
            .Where(p => p.TournamentId == q.CurrentTournamentId)
            .Select(p => p.UserId)
            .ToListAsync(ct);
        var already = alreadyIn.ToHashSet();

        return await _db.TournamentPlayers
            .Where(p => p.TournamentId == q.SeedTournamentId)
            .Include(p => p.User)
            .OrderByDescending(p => p.Score)
            .Select(p => new SeedCandidate(
                p.UserId, p.User.UserName!, p.User.Name, p.Score, already.Contains(p.UserId)))
            .ToListAsync(ct);
    }
}
