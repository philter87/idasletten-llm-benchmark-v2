using MediatR;

namespace Idasletten.Features.Matches.Commands.AddPlannedMatch;

/// <summary>Plans a single future match: just team compositions by initials, no score yet.</summary>
public record AddPlannedMatchCommand(Guid TournamentId, IReadOnlyList<IReadOnlyList<string>> Teams) : IRequest<Guid>;
