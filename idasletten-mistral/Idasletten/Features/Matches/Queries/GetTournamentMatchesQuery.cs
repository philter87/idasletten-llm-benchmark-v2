using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using MediatR;

namespace Idasletten.Features.Matches.Queries;

public record GetTournamentMatchesQuery(Guid TournamentId, MatchState? State = null) : IRequest<List<TournamentMatch>>;
