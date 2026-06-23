using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries.GetTournament;

public record GetTournamentQuery(Guid TournamentId) : IRequest<Tournament?>;
