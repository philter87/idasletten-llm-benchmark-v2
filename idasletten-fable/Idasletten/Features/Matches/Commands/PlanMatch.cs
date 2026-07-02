using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

/// <summary>Plans a single future match from lists of player initials.</summary>
public record PlanMatchCommand(Guid TournamentId, List<List<string>> Teams) : IRequest<TournamentMatch>;

public record MatchPlanned(Guid TournamentId, Guid MatchId) : INotification;

public class PlanMatchHandler(AppDbContext db, IMediator mediator, IPublisher publisher)
    : IRequestHandler<PlanMatchCommand, TournamentMatch>
{
    public async Task<TournamentMatch> Handle(PlanMatchCommand request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.FindAsync([request.TournamentId], ct)
            ?? throw new InvalidOperationException($"Tournament {request.TournamentId} not found.");

        var teams = await TeamResolver.ResolveTeams(
            db, mediator, tournament,
            request.Teams.Select(t => (IReadOnlyList<string>)t).ToList(), ct);

        var maxOrder = await db.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .Select(m => (int?)m.Order)
            .MaxAsync(ct) ?? 0;

        var match = new TournamentMatch
        {
            TournamentId = tournament.Id,
            Order = maxOrder + 1,
            State = MatchState.Planned,
            Results = teams.Select(t => new TournamentTeamMatchResult
            {
                TournamentId = tournament.Id,
                TeamId = t.Id,
                Team = t
            }).ToList()
        };
        db.TournamentMatches.Add(match);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new MatchPlanned(tournament.Id, match.Id), ct);
        return match;
    }
}
