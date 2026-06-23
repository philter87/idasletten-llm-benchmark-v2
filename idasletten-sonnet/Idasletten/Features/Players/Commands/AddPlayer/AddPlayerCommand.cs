using MediatR;

namespace Idasletten.Features.Players.Commands.AddPlayer;

public record AddPlayerCommand(Guid TournamentId, string Initials, string? Name = null) : IRequest<Guid>;
