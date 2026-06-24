using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Players.Commands;

public record AddPlayerToTournamentCommand(
    Guid TournamentId,
    string Initials,
    string? Name = null) : IRequest<TournamentPlayer>;
