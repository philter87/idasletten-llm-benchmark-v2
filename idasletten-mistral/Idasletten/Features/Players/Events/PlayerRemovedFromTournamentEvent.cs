using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Players.Events;

public record PlayerRemovedFromTournamentEvent(TournamentPlayer TournamentPlayer, Guid TournamentId) : INotification;
