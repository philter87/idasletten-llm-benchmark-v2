using Idasletten.Features.Matches.Events;
using Idasletten.Features.Players;
using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

/// <summary>
/// The "plan several matches" dialog. Every player gets <see cref="GamesPerPlayer"/> games, which is
/// what decides how many matches are created. When the tournament has a seed tournament the ranking
/// from that tournament decides who plays with whom.
/// </summary>
public record PlanMatches(
    Guid TournamentId,
    int GamesPerPlayer,
    bool FixedTeams = false,
    SeedingType Seeding = SeedingType.Random,
    Guid? SeedTournamentId = null,
    int? RandomSeed = null) : IRequest<int>;

public class PlanMatchesHandler(AppDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<PlanMatches, int>
{
    public async Task<int> Handle(PlanMatches request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new ArgumentException("Unknown tournament.", nameof(request));

        if (request.SeedTournamentId is { } seedId &&
            tournament.SeedTournamentId is null &&
            tournament.CanBeSeeded)
        {
            await sender.Send(new SetSeedTournament(tournament.Id, seedId), cancellationToken);
        }

        var ranked = await RankPlayersAsync(tournament.Id, tournament.Players, cancellationToken);

        var random = request.RandomSeed is { } seed ? new Random(seed) : new Random();
        var games = MatchPlanner.Plan(
            ranked, tournament.TeamSize, request.GamesPerPlayer, request.FixedTeams,
            request.Seeding, random);

        var nextOrder = await db.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .Select(m => (int?)m.Order)
            .MaxAsync(cancellationToken) ?? 0;

        foreach (var game in games)
        {
            var match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                Order = ++nextOrder,
                State = MatchState.Planned,
            };

            foreach (var plannedTeam in game.Teams)
            {
                var team = await MatchTeams.GetOrCreateTeamAsync(
                    db, tournament, plannedTeam.PlayerIds, cancellationToken);

                match.Results.Add(new TournamentTeamMatchResult
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    TournamentId = tournament.Id,
                    TeamId = team.Id,
                });
            }

            db.TournamentMatches.Add(match);
        }

        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new MatchesPlanned(tournament.Id, games.Count, request.Seeding, request.FixedTeams),
            cancellationToken);

        return games.Count;
    }

    /// <summary>
    /// Best player first. The seed tournament decides the ranking when there is one - players who did
    /// not play there are put last, ranked by their score in this tournament.
    /// </summary>
    private async Task<List<Guid>> RankPlayersAsync(
        Guid tournamentId, List<TournamentPlayer> players, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .AsNoTracking()
            .FirstAsync(t => t.Id == tournamentId, cancellationToken);

        if (tournament.SeedTournamentId is not { } seedId)
        {
            return ScoreEngine.Rank(players).Select(p => p.Id).ToList();
        }

        var seedScores = await db.TournamentPlayers
            .AsNoTracking()
            .Where(p => p.TournamentId == seedId)
            .ToDictionaryAsync(p => p.UserId, p => p.Score, cancellationToken);

        return players
            .OrderByDescending(p => seedScores.ContainsKey(p.UserId))
            .ThenByDescending(p => seedScores.TryGetValue(p.UserId, out var score) ? score : 0)
            .ThenByDescending(p => p.Score)
            .Select(p => p.Id)
            .ToList();
    }
}
