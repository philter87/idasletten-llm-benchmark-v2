using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Users.Queries;

public record GetUserQuery(Guid UserId) : IRequest<User?>;
