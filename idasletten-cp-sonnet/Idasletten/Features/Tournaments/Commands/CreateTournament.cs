using Idasletten.Features.Tournaments.Entities;
using Idasletten.Features.Tournaments.Events;
using Idasletten.Shared.Data;
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
    Guid? SeedTournamentId,
    Guid? ParentTournamentId) : IRequest<Guid>;

public sealed class CreateTournamentHandler(AppDbContext db, IMediator mediator) : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _db = db;
    private readonly IMediator _mediator = mediator;

    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var name = NormalizeRequired(request.Name, nameof(request.Name));

        if (request.TeamSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TeamSize), "Team size must be greater than zero.");
        }

        if (request.PointsToWin <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PointsToWin), "Points to win must be greater than zero.");
        }

        if (request.MaxPlayerCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxPlayerCount), "Max player count must be greater than zero when specified.");
        }

        Tournament? parentTournament = null;
        if (request.ParentTournamentId.HasValue)
        {
            parentTournament = await _db.Tournaments
                .Include(tournament => tournament.Players)
                .FirstOrDefaultAsync(tournament => tournament.Id == request.ParentTournamentId.Value, cancellationToken);

            if (parentTournament is null)
            {
                throw new InvalidOperationException($"Tournament '{request.ParentTournamentId}' was not found.");
            }
        }

        var tournament = new Tournament
        {
            Name = name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            SeedTournamentId = request.SeedTournamentId,
            ParentTournamentId = request.ParentTournamentId,
            RoundNumber = parentTournament is null
                ? null
                : (parentTournament.RoundNumber.HasValue ? parentTournament.RoundNumber.Value + 1 : 1)
        };

        _db.Tournaments.Add(tournament);

        if (parentTournament is not null)
        {
            foreach (var parentPlayer in parentTournament.Players)
            {
                _db.TournamentPlayers.Add(new TournamentPlayer
                {
                    TournamentId = tournament.Id,
                    UserId = parentPlayer.UserId,
                    Score = GetInitialScore(request.ScoreSystem),
                    WinCount = 0,
                    LoseCount = 0,
                    MatchCount = 0,
                    Lives = DefaultLives,
                    PointsWon = 0,
                    PointsLost = 0,
                    ScoreDiff = 0
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _mediator.Publish(new TournamentCreated(tournament.Id, tournament.Name), cancellationToken);

        return tournament.Id;
    }

    private const int DefaultLives = 3;

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }

    private static double GetInitialScore(ScoreSystem scoreSystem) => scoreSystem switch
    {
        ScoreSystem.Elo => 1000d,
        ScoreSystem.TrueSkill => (25d - (3d * 8.333d)) * 100d,
        ScoreSystem.Lives => DefaultLives,
        ScoreSystem.WinCount => 0d,
        _ => 0d
    };
}
