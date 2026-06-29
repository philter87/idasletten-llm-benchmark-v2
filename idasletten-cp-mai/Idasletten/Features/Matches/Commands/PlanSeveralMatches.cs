using Idasletten.Features.Players;
using Idasletten.Features.Players.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public enum SeedingType
{
    Random,
    Equality,
    Fair
}

public record PlanSeveralMatchesCommand(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeam,
    SeedingType Seeding,
    Guid? SeedTournamentId = null) : IRequest<List<Guid>>;

public class PlanSeveralMatchesHandler : IRequestHandler<PlanSeveralMatchesCommand, List<Guid>>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IMediator _mediator;

    public PlanSeveralMatchesHandler(Shared.Data.ApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<List<Guid>> Handle(PlanSeveralMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { request.TournamentId }, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        var seedId = request.SeedTournamentId ?? tournament.SeedTournamentId;
        var seedPlayerIds = seedId.HasValue
            ? await _db.TournamentPlayers
                .Where(p => p.TournamentId == seedId.Value)
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.PointsWon - p.PointsLost)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken)
            : await _db.TournamentPlayers
                .Where(p => p.TournamentId == request.TournamentId)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

        if (seedPlayerIds.Count < tournament.TeamSize * 2)
            throw new InvalidOperationException("Not enough players to plan a match");

        int slotsPerMatch = tournament.TeamSize * 2;
        int totalMatches = (seedPlayerIds.Count * request.GamesPerPlayer) / slotsPerMatch;
        if (totalMatches == 0) totalMatches = 1;

        var createdMatchIds = new List<Guid>();
        var rng = new Random();

        for (int m = 0; m < totalMatches; m++)
        {
            List<Guid> ordered;
            if (request.FixedTeam && m > 0)
            {
                // Reuse same team composition, but maybe alternate sides
                ordered = seedPlayerIds.ToList();
            }
            else
            {
                ordered = request.Seeding switch
                {
                    SeedingType.Random => seedPlayerIds.OrderBy(_ => rng.Next()).ToList(),
                    SeedingType.Equality => EqualityOrder(seedPlayerIds),
                    SeedingType.Fair => FairOrder(seedPlayerIds),
                    _ => seedPlayerIds.ToList()
                };
            }

            var matchUsers = ordered.Take(slotsPerMatch).ToList();
            var teamAUsers = matchUsers.Take(tournament.TeamSize).Select(u => u.ToString()).ToList(); // we need initials, not user ids
            var teamBUsers = matchUsers.Skip(tournament.TeamSize).Take(tournament.TeamSize).Select(u => u.ToString()).ToList();

            var initialsByUserId = await _db.Users
                .Where(u => matchUsers.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

            var matchId = await _mediator.Send(new PlanMatchCommand(request.TournamentId, new List<List<string>>
            {
                teamAUsers.Select(id => initialsByUserId[Guid.Parse(id)]).ToList(),
                teamBUsers.Select(id => initialsByUserId[Guid.Parse(id)]).ToList()
            }), cancellationToken);

            createdMatchIds.Add(matchId);
        }

        return createdMatchIds;
    }

    private static List<Guid> EqualityOrder(List<Guid> players)
    {
        // Pair best with worst for balanced teams of two, then fold into matches.
        var result = new List<Guid>();
        int n = players.Count;
        int i = 0, j = n - 1;
        while (i <= j)
        {
            result.Add(players[i]);
            if (i != j) result.Add(players[j]);
            i++;
            j--;
        }
        return result;
    }

    private static List<Guid> FairOrder(List<Guid> players)
    {
        // Split into top and bottom halves and pair top[i] with bottom[i].
        var result = new List<Guid>();
        int n = players.Count;
        int half = (n + 1) / 2;
        var top = players.Take(half).ToList();
        var bottom = players.Skip(half).ToList();
        for (int i = 0; i < top.Count; i++)
        {
            result.Add(top[i]);
            if (i < bottom.Count) result.Add(bottom[i]);
        }
        return result;
    }
}
