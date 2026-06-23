using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;

namespace Idasletten.Features.Matches.Queries.GetMatches;

public record GetMatchesQuery(Guid TournamentId, MatchState? State = null) : IRequest<List<TournamentMatch>>;
