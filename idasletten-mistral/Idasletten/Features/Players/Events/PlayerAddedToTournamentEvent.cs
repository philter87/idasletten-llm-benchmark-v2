using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Players.Events;

public record PlayerAddedToTournamentEvent(TournamentPlayer TournamentPlayer) : INotification;
