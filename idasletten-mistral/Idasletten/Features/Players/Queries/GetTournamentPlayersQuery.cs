using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Players.Queries;

public record GetTournamentPlayersQuery(Guid TournamentId) : IRequest<List<TournamentPlayer>>;
