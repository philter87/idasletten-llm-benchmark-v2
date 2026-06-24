using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries;

public record GetPublicTournamentsQuery() : IRequest<List<Tournament>>;
