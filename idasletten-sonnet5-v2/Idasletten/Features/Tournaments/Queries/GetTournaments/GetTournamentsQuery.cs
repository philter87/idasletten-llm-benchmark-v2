using Idasletten.Shared.Entities;
using MediatR;

namespace Idasletten.Features.Tournaments.Queries.GetTournaments;

public enum TournamentListScope
{
    /// <summary>Non-archived, public tournaments — shown on the home page.</summary>
    Public,

    /// <summary>Every tournament (archived and private included) — the historical listing.</summary>
    All,
}

public record GetTournamentsQuery(TournamentListScope Scope) : IRequest<IReadOnlyList<TournamentSummaryDto>>;

public record TournamentSummaryDto(
    Guid Id,
    string Name,
    int TeamSize,
    ScoreSystem ScoreSystem,
    bool IsArchived,
    bool IsPublic,
    int PlayerCount,
    int? RoundNumber);
