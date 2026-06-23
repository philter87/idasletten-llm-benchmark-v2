using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Players.Queries.GetTournamentPlayers;

public record GetTournamentPlayersQuery(Guid TournamentId) : IRequest<List<TournamentPlayer>>;
