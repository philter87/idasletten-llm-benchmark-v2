using Idasletten.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.PlanSeveralMatches;

public class PlanSeveralMatchesHandler(IdaslettenDbContext db, IPublisher publisher)
    : IRequestHandler<PlanSeveralMatchesCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(PlanSeveralMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .Include(t => t.Matches)
            .FirstAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (request.SeedTournamentId is { } seedId && tournament.SeedTournamentId is null)
        {
            if (tournament.ParentTournamentId is not null)
            {
                throw new InvalidOperationException("A tournament with a parent cannot be seeded.");
            }
            tournament.SeedTournamentId = seedId;
        }

        var rankedPlayerIds = await RankPlayersAsync(tournament, request.SeedingType, cancellationToken);

        if (rankedPlayerIds.Count < tournament.TeamSize * 2)
        {
            throw new InvalidOperationException("Not enough players to plan a match.");
        }

        var slotsPerMatch = tournament.TeamSize * 2;
        var totalSlotsNeeded = rankedPlayerIds.Count * request.GamesPerPlayer;
        var matchesNeeded = (int)Math.Ceiling(totalSlotsNeeded / (double)slotsPerMatch);

        var fixedTeams = request.FixedTeams
            ? TeamSeeder.BuildTeams(rankedPlayerIds, request.SeedingType, tournament.TeamSize)
            : null;

        var nextOrder = tournament.Matches.Count == 0 ? 1 : tournament.Matches.Max(m => m.Order) + 1;
        var createdMatchIds = new List<Guid>();
        var roundOffset = 0;

        while (createdMatchIds.Count < matchesNeeded)
        {
            var teams = fixedTeams ?? TeamSeeder.BuildTeams(rankedPlayerIds, request.SeedingType, tournament.TeamSize);
            var teamCount = teams.Count;
            if (teamCount < 2)
            {
                break;
            }

            for (var pairIndex = 0; pairIndex < teamCount / 2 && createdMatchIds.Count < matchesNeeded; pairIndex++)
            {
                var teamAIndex = pairIndex;
                var teamBIndex = (teamCount - 1 - pairIndex + roundOffset) % teamCount;
                if (teamAIndex == teamBIndex)
                {
                    continue;
                }

                var match = new TournamentMatch
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournament.Id,
                    Order = nextOrder++,
                    State = MatchState.Planned,
                };
                db.TournamentMatches.Add(match);

                AddTeam(db, match, teams[teamAIndex], 1);
                AddTeam(db, match, teams[teamBIndex], 2);

                createdMatchIds.Add(match.Id);
            }

            roundOffset++;
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new MatchesPlanned(tournament.Id, createdMatchIds), cancellationToken);

        return createdMatchIds;
    }

    private async Task<List<Guid>> RankPlayersAsync(Tournament tournament, SeedingType seedingType, CancellationToken cancellationToken)
    {
        if (tournament.SeedTournamentId is not { } seedTournamentId || seedingType == SeedingType.Random)
        {
            return tournament.Players.OrderByDescending(p => p.Score).Select(p => p.Id).ToList();
        }

        var seedPlayerUserIds = await db.TournamentPlayers
            .Where(p => p.TournamentId == seedTournamentId)
            .OrderByDescending(p => p.Score)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        var byUserId = tournament.Players.ToDictionary(p => p.UserId);
        var ranked = seedPlayerUserIds.Where(byUserId.ContainsKey).Select(uid => byUserId[uid].Id).ToList();

        // Players not present in the seed tournament are appended at the bottom of the ranking.
        ranked.AddRange(tournament.Players.Select(p => p.Id).Except(ranked));

        return ranked;
    }

    private static void AddTeam(IdaslettenDbContext db, TournamentMatch match, List<Guid> playerIds, int number)
    {
        var team = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            Number = number,
            Name = $"Team {number}",
        };
        foreach (var playerId in playerIds)
        {
            team.TeamPlayers.Add(new TournamentTeamPlayer { TeamId = team.Id, TournamentPlayerId = playerId });
        }
        db.TournamentTeams.Add(team);
        match.Teams.Add(team);
    }
}
