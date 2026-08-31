using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Tournaments;
using Idasletten.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands.AddPlayerByUser;

/// <summary>Adds an existing user (e.g. from a previous tournament) to a tournament.</summary>
public sealed record AddPlayerByUserCommand(Guid TournamentId, Guid UserId) : IRequest<PlayerRowDto>;

public sealed class AddPlayerByUserCommandHandler : IRequestHandler<AddPlayerByUserCommand, PlayerRowDto>
{
    private readonly AppDbContext _db;
    private readonly ScoringEngine _scoring;
    private readonly IPublisher _publisher;

    public AddPlayerByUserCommandHandler(AppDbContext db, ScoringEngine scoring, IPublisher publisher)
    {
        _db = db;
        _scoring = scoring;
        _publisher = publisher;
    }

    public async Task<PlayerRowDto> Handle(AddPlayerByUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!exists) throw new FeatureException("User not found.");
        var tournament = await AddPlayerCommandHandler.GetActiveTournamentAsync(_db, request.TournamentId, cancellationToken);
        return await AddPlayerCommandHandler.AddUserAsync(_db, _scoring, tournament, request.UserId, _publisher, cancellationToken);
    }
}
