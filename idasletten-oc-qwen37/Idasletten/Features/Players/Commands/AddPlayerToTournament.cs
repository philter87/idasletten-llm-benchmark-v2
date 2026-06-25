using Idasletten.Models;
using MediatR;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerToTournamentCommand(Guid TournamentId, string Username, string? Name) : IRequest<Guid>;
