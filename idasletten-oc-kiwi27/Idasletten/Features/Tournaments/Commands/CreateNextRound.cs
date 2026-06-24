using Idasletten.Features.Players;
using Idasletten.Features.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public record CreateNextRoundCommand(Guid ParentTournamentId, string Name, int? TopPlayerCount = null) : IRequest<Guid>;

public class CreateNextRoundHandler : IRequestHandler<CreateNextRoundCommand, Guid>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IPublisher _publisher;
    private readonly IScoreCalculatorFactory _scoreFactory;

    public CreateNextRoundHandler(Shared.Data.ApplicationDbContext db, IPublisher publisher, IScoreCalculatorFactory scoreFactory)
    {
        _db = db;
        _publisher = publisher;
        _scoreFactory = scoreFactory;
    }

    public async Task<Guid> Handle(CreateNextRoundCommand request, CancellationToken cancellationToken)
    {
        var parent = await _db.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.ParentTournamentId, cancellationToken)
            ?? throw new InvalidOperationException("Parent tournament not found");

        var child = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TeamSize = parent.TeamSize,
            PointsToWin = parent.PointsToWin,
            ScoreSystem = parent.ScoreSystem,
            MaxPlayerCount = parent.MaxPlayerCount,
            IsPublic = parent.IsPublic,
            ParentTournamentId = parent.Id,
            RoundNumber = parent.RoundNumber + 1
        };

        _db.Tournaments.Add(child);
        await _db.SaveChangesAsync(cancellationToken);

        var parentPlayers = await _db.TournamentPlayers
            .Where(p => p.TournamentId == parent.Id)
            .Include(p => p.User)
            .ToListAsync(cancellationToken);

        var calculator = _scoreFactory.Create(child.ScoreSystem);

        var ordered = parentPlayers
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ToList();

        var top = request.TopPlayerCount.HasValue ? ordered.Take(request.TopPlayerCount.Value).ToList() : ordered;

        foreach (var parentPlayer in top)
        {
            _db.TournamentPlayers.Add(new TournamentPlayer
            {
                Id = Guid.NewGuid(),
                TournamentId = child.Id,
                UserId = parentPlayer.UserId,
                Score = calculator.InitialScore,
                Lives = LivesCalculator.InitialLives,
                TrueSkillMean = 25,
                TrueSkillStdDev = 25.0 / 3.0
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new TournamentCreated { TournamentId = child.Id }, cancellationToken);
        return child.Id;
    }
}
