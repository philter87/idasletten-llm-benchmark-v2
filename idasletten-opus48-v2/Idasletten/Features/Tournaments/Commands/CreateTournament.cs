using Idasletten.Data;
using Idasletten.Shared.Domain;
using Idasletten.Shared.Events;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    int TeamSize,
    int PointsToWin,
    ScoreSystem ScoreSystem,
    int? MaxPlayerCount,
    bool IsPublic,
    Guid? SeedTournamentId = null,
    Guid? ParentTournamentId = null) : IRequest<Guid>;

public record TournamentCreated(Guid TournamentId, string Name) : IDomainEvent;

public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ScoreService _scores;

    public CreateTournamentHandler(AppDbContext db, IPublisher publisher, ScoreService scores)
    {
        _db = db;
        _publisher = publisher;
        _scores = scores;
    }

    public async Task<Guid> Handle(CreateTournamentCommand cmd, CancellationToken ct)
    {
        int? roundNumber = null;
        Guid? seedId = cmd.SeedTournamentId;

        if (cmd.ParentTournamentId is { } parentId)
        {
            // A child round may not be seeded; it derives players from its parent instead.
            seedId = null;
            var parentRound = await _db.Tournaments
                .Where(t => t.Id == parentId)
                .Select(t => t.RoundNumber)
                .FirstOrDefaultAsync(ct);
            roundNumber = (parentRound ?? 1) + 1;
        }

        var tournament = new Tournament
        {
            Name = cmd.Name,
            TeamSize = cmd.TeamSize <= 0 ? 2 : cmd.TeamSize,
            PointsToWin = cmd.PointsToWin <= 0 ? 5 : cmd.PointsToWin,
            ScoreSystem = cmd.ScoreSystem,
            MaxPlayerCount = cmd.MaxPlayerCount,
            IsPublic = cmd.IsPublic,
            SeedTournamentId = seedId,
            ParentTournamentId = cmd.ParentTournamentId,
            RoundNumber = roundNumber
        };
        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync(ct);

        // A child round carries over the parent's players with scores reset to the baseline.
        if (cmd.ParentTournamentId is { } pid)
        {
            double initial = _scores.CalculatorFor(tournament.ScoreSystem).InitialScore;
            var parentPlayers = await _db.TournamentPlayers
                .Where(p => p.TournamentId == pid)
                .Select(p => p.UserId)
                .ToListAsync(ct);
            foreach (var userId in parentPlayers)
            {
                _db.TournamentPlayers.Add(new TournamentPlayer
                {
                    TournamentId = tournament.Id,
                    UserId = userId,
                    Score = initial,
                    Lives = 3
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        await _publisher.Publish(new TournamentCreated(tournament.Id, tournament.Name), ct);
        return tournament.Id;
    }
}
