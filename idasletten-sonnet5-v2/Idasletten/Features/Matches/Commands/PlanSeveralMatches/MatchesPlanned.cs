using MediatR;

namespace Idasletten.Features.Matches.Commands.PlanSeveralMatches;

public record MatchesPlanned(Guid TournamentId, IReadOnlyList<Guid> MatchIds) : INotification;
