using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournamentQuery(Guid TournamentId) : IRequest<Tournament?>;
