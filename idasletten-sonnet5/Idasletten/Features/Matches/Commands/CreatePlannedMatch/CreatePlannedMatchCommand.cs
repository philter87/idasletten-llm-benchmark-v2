using MediatR;

namespace Idasletten.Features.Matches.Commands.CreatePlannedMatch;

/// Creates a blank Planned match (no teams yet) and returns its Id, so the caller can
/// redirect to the create-match page for that Id — the same page is then reused whether the
/// match ends up freshly planned, pre-filled, or (once Done) edited.
public record CreatePlannedMatchCommand(Guid TournamentId) : IRequest<Guid>;
