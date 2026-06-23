using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record MatchDetailDto(Guid Id, Guid TournamentId, int Order, Features.Matches.Entities.MatchState State, DateTimeOffset? PlayedAt, IReadOnlyList<TeamResultDto> Teams);

public record GetMatchQuery(Guid MatchId) : IRequest<MatchDetailDto?>;

public sealed class GetMatchHandler(AppDbContext db, IMediator mediator) : IRequestHandler<GetMatchQuery, MatchDetailDto?>
{
    private readonly AppDbContext _db = db;

    public async Task<MatchDetailDto?> Handle(GetMatchQuery request, CancellationToken cancellationToken)
    {
        var match = await _db.TournamentMatches
            .AsNoTracking()
            .Where(value => value.Id == request.MatchId)
            .Include(value => value.TeamResults)
                .ThenInclude(result => result.Team)
                    .ThenInclude(team => team.TeamPlayers)
                        .ThenInclude(teamPlayer => teamPlayer.Player)
                            .ThenInclude(player => player.User)
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return null;
        }

        var summary = GetMatchesHandler.MapMatch(match);
        return new MatchDetailDto(summary.Id, summary.TournamentId, summary.Order, summary.State, summary.PlayedAt, summary.Teams);
    }
}
