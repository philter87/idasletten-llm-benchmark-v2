using MediatR;

namespace Idasletten.Features.Matches.Commands.SaveMatchResult;

public record MatchResultSaved(Guid TournamentId, Guid MatchId) : INotification;
