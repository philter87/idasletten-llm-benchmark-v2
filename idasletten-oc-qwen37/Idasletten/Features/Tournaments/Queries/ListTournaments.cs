using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries;

public record ListTournamentsQuery(bool IncludeArchived = false, bool IncludePrivate = false) : IRequest<List<Tournament>>;
