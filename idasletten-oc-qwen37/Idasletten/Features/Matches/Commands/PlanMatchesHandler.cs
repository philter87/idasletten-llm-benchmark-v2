using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class PlanMatchesHandler : IRequestHandler<PlanMatchesCommand>
{
    private readonly AppDbContext _db;

    public PlanMatchesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(PlanMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .Include(t => t.Players)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        var players = tournament.Players.ToList();
        if (players.Count < tournament.TeamSize * 2)
            throw new InvalidOperationException("Not enough players");

        if (request.SeedTournamentId.HasValue)
        {
            var seedPlayers = await _db.TournamentPlayers
                .Include(p => p.User)
                .Where(p => p.TournamentId == request.SeedTournamentId)
                .OrderByDescending(p => p.Score)
                .ToListAsync(cancellationToken);

            players = players.OrderBy(p =>
            {
                var seedPlayer = seedPlayers.FirstOrDefault(sp => sp.UserId == p.UserId);
                return seedPlayer != null ? seedPlayers.IndexOf(seedPlayer) : int.MaxValue;
            }).ToList();
        }

        var totalMatches = (players.Count * request.GamesPerPlayer) / tournament.TeamSize;
        var existingMatches = await _db.TournamentMatches.CountAsync(m => m.TournamentId == request.TournamentId, cancellationToken);
        var order = existingMatches + 1;

        var random = new Random();

        for (int i = 0; i < totalMatches; i++)
        {
            var match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = request.TournamentId,
                Order = order++,
                State = MatchState.Planned
            };

            var shuffledPlayers = request.SeedingType == SeedingType.Random
                ? players.OrderBy(_ => random.Next()).ToList()
                : players;

            var teamSize = tournament.TeamSize;
            var teams = new List<List<TournamentPlayer>>();

            for (int t = 0; t < 2; t++)
            {
                var team = new List<TournamentPlayer>();
                for (int p = 0; p < teamSize; p++)
                {
                    var playerIndex = request.SeedingType switch
                    {
                        SeedingType.Equality => (t * teamSize + p) % players.Count,
                        SeedingType.Fair => GetFairIndex(t, p, teamSize, players.Count),
                        _ => (t * teamSize + p) % shuffledPlayers.Count
                    };
                    team.Add(shuffledPlayers[playerIndex]);
                }
                teams.Add(team);
            }

            var teamNumber = (await _db.TournamentTeams.CountAsync(t => t.TournamentId == request.TournamentId, cancellationToken)) + 1;

            foreach (var teamPlayers in teams)
            {
                var team = new TournamentTeam
                {
                    Id = Guid.NewGuid(),
                    TournamentId = request.TournamentId,
                    Name = $"Team {teamNumber}",
                    Number = teamNumber
                };
                foreach (var player in teamPlayers)
                {
                    team.Players.Add(player);
                }
                _db.TournamentTeams.Add(team);

                match.TeamResults.Add(new TournamentTeamMatchResult
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    TournamentId = request.TournamentId,
                    TeamId = team.Id,
                    GoalsWon = 0,
                    GoalsLost = 0
                });

                teamNumber++;
            }

            _db.TournamentMatches.Add(match);

            if (!request.FixedTeams)
            {
                players = players.OrderBy(_ => random.Next()).ToList();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private int GetFairIndex(int team, int position, int teamSize, int playerCount)
    {
        var halfSize = playerCount / 2;
        if (team == 0)
        {
            return position % halfSize;
        }
        else
        {
            return halfSize + (position % (playerCount - halfSize));
        }
    }
}
