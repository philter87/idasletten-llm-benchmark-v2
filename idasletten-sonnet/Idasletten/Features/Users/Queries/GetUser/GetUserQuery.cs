using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Queries.GetUser;

public record GetUserQuery(Guid UserId) : IRequest<User?>;
