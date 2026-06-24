using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Users.Queries;

public record GetUserByIdQuery(string UserId) : IRequest<User?>;
