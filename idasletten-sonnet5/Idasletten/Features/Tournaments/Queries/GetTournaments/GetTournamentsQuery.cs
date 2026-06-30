using MediatR;

namespace Idasletten.Features.Tournaments.Queries.GetTournaments;

/// IncludeChildren=false (the default) hides tournaments that are a later round of another
/// tournament (ParentTournamentId set), per spec: "When viewing all tournaments, we should
/// not view child tournaments by default."
public record GetTournamentsQuery(
    bool? IsPublic = null,
    bool IncludeArchived = true,
    bool IncludeChildren = false) : IRequest<IReadOnlyList<TournamentSummaryDto>>;

public record TournamentSummaryDto(
    Guid Id,
    string Name,
    ScoreSystem ScoreSystem,
    bool IsArchived,
    bool IsPublic,
    int PlayerCount,
    DateTime CreatedAtUtc);
