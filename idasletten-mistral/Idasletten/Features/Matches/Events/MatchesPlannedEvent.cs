using Idasletten.Shared.Data.Entities;
using MediatR;

namespace Idasletten.Features.Matches.Events;

public record MatchesPlannedEvent(List<TournamentMatch> Matches, Guid TournamentId) : INotification;
