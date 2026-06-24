using Idasletten.Features.Scoring;
using Idasletten.Features.Users.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerToTournamentCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<Guid>;

public class PlayerAddedToTournament : INotification
{
    public Guid TournamentId { get; set; }
    public Guid UserId { get; set; }
}

public class AddPlayerToTournamentHandler : IRequestHandler<AddPlayerToTournamentCommand, Guid>
{
    private readonly Shared.Data.ApplicationDbContext _db;
    private readonly IMediator _mediator;
    private readonly IPublisher _publisher;
    private readonly IScoreCalculatorFactory _scoreFactory;

    public AddPlayerToTournamentHandler(Shared.Data.ApplicationDbContext db, IMediator mediator, IPublisher publisher, IScoreCalculatorFactory scoreFactory)
    {
        _db = db;
        _mediator = mediator;
        _publisher = publisher;
        _scoreFactory = scoreFactory;
    }

    public async Task<Guid> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var userId = await _mediator.Send(new Users.Commands.CreateUserCommand(request.Initials, request.Name), cancellationToken);

        var existing = await _db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == request.TournamentId && p.UserId == userId, cancellationToken);
        if (existing != null) return existing.Id;

        var tournament = await _db.Tournaments.FindAsync(new object[] { request.TournamentId }, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        if (tournament.MaxPlayerCount.HasValue && await _db.TournamentPlayers.CountAsync(p => p.TournamentId == request.TournamentId, cancellationToken) >= tournament.MaxPlayerCount.Value)
            throw new InvalidOperationException("Tournament is full");

        var calculator = _scoreFactory.Create(tournament.ScoreSystem);
        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            TournamentId = request.TournamentId,
            UserId = userId,
            Score = calculator.InitialScore,
            Lives = LivesCalculator.InitialLives,
            TrueSkillMean = 25,
            TrueSkillStdDev = 25.0 / 3.0
        };

        _db.TournamentPlayers.Add(player);
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new PlayerAddedToTournament { TournamentId = request.TournamentId, UserId = userId }, cancellationToken);
        return player.Id;
    }
}
