using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Matches.Queries;

public record ListMatchesQuery(Guid TournamentId, MatchState? State = null) : IRequest<List<TournamentMatch>>;
