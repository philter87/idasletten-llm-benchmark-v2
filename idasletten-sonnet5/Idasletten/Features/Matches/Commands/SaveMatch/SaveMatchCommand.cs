using MediatR;

namespace Idasletten.Features.Matches.Commands.SaveMatch;

public record TeamInput(IReadOnlyList<string> Initials, int Score);

/// Saves a match's team composition and, when RecordResult is true, its result — marking the
/// match Done and triggering a full tournament score recalculation. Initials that don't match
/// an existing user auto-create one (and a TournamentPlayer row). Reused for brand-new planned
/// matches, edits to a still-Planned match, and (when the caller is authorized) edits to a
/// Done match.
public record SaveMatchCommand(Guid MatchId, Guid TournamentId, IReadOnlyList<TeamInput> Teams, bool RecordResult)
    : IRequest;
