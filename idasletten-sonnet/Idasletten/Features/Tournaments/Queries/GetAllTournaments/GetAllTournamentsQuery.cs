using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries.GetAllTournaments;

public record GetAllTournamentsQuery(bool IncludeChildren = false, bool IncludeArchived = false) : IRequest<List<Tournament>>;
