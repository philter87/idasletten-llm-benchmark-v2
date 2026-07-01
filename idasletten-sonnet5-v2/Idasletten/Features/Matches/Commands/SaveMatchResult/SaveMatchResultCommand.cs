using MediatR;

namespace Idasletten.Features.Matches.Commands.SaveMatchResult;

public record MatchTeamInput(IReadOnlyList<string> PlayerInitials, int GoalsWon);

/// <summary>
/// Creates or updates a match result. The match Id is generated client-side before
/// navigating to the create-match page, so the same page/command handles: creating a
/// brand-new result, completing a previously-planned match (pre-filled), and editing an
/// already-Done match (which requires the caller to have verified login and sets
/// <see cref="IsEditAuthorized"/>).
/// </summary>
public record SaveMatchResultCommand(
    Guid MatchId,
    Guid TournamentId,
    IReadOnlyList<MatchTeamInput> Teams,
    bool IsEditAuthorized) : IRequest;
