using MediatR;

namespace Idasletten.Features.Matches.Commands.AddPlannedMatch;

public record MatchPlanned(Guid TournamentId, Guid MatchId) : INotification;
