using MediatR;

namespace Idasletten.Features.Matches.Events;

public record MatchPlanned(Guid MatchId, Guid TournamentId) : INotification;
