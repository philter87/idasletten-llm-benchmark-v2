using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Users.Queries;

public record GetUserByUsernameQuery(string Username) : IRequest<User?>;
public record GetUserByIdQuery(Guid UserId) : IRequest<User?>;
