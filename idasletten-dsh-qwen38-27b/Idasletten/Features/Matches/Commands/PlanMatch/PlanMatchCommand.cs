using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Matches.Events;
using Idasletten.Features.Users.Commands.FindOrCreateUser;
using Idasletten.Models;
using Idasletten.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands.PlanMatch;

/// <summary>Plans a single match (State=Planned) from teams of initials.</summary>
public sealed record PlanMatchCommand(Guid TournamentId, IReadOnlyList<IReadOnlyList<string>> TeamInitials) : IRequest<Guid>;

public sealed class PlanMatchCommandHandler : IRequestHandler<PlanMatchCommand, Guid>
{
    private readonly AppDbContext _db;
    private readonly ScoringEngine _scoring;
    private readonly IMediator _mediator;
    private readonly IPublisher _publisher;

    public PlanMatchCommandHandler(AppDbContext db, ScoringEngine scoring, IMediator mediator, IPublisher publisher)
    {
        _db = db;
        _scoring = scoring;
        _mediator = mediator;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(PlanMatchCommand request, CancellationToken cancellationToken)
    {
        if (request.TeamInitials is null || request.TeamInitials.Count < 2 || request.TeamInitials.Count > 4)
            throw new FeatureException("A match needs between 2 and 4 teams.");

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new FeatureException("Tournament not found.");
        if (tournament.IsArchived)
            throw new FeatureException("This tournament is archived; matches can no longer be planned.");

        var allInitials = request.TeamInitials.SelectMany(t => t).ToList();
        if (allInitials.Select(i => FindOrCreateUserCommandHandler.Normalize(i)).Distinct().Count() != allInitials.Count)
            throw new FeatureException("A player cannot play for two teams in the same match.");

        var resolved = new List<List<Guid>>();
        foreach (var teamInitials in request.TeamInitials)
            resolved.Add(await MatchSupport.ResolveInitialsAsync(_db, _scoring, _mediator, _publisher, tournament, teamInitials, true, cancellationToken));

        var match = new TournamentMatch
        {
            TournamentId = tournament.Id,
            Order = await MatchSupport.NextOrderAsync(_db, tournament.Id, cancellationToken),
            State = MatchState.Planned
        };
        _db.TournamentMatches.Add(match);
        await _db.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < resolved.Count; i++)
        {
            var team = await MatchSupport.CreateTeamAsync(_db, tournament, i + 1, resolved[i], cancellationToken);
            _db.MatchTeams.Add(new MatchTeam { MatchId = match.Id, TeamId = team.Id });
        }
        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new MatchPlanned(match.Id, tournament.Id), cancellationToken);
        return match.Id;
    }
}
