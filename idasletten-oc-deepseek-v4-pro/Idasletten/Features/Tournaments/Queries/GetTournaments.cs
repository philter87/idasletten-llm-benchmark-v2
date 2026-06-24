using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournamentsQuery(bool IncludeArchived = false) : IRequest<List<Tournament>>;

public record GetPublicTournamentsQuery : IRequest<List<Tournament>>;

public record GetTournamentByIdQuery(Guid TournamentId) : IRequest<Tournament?>;
