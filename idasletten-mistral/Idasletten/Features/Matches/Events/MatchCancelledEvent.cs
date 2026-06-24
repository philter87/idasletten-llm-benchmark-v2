using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Matches.Events;

public record MatchCancelledEvent(TournamentMatch Match) : INotification;
