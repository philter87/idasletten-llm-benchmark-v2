using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchQuery(Guid MatchId) : IRequest<TournamentMatch?>;
