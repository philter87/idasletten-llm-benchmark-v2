using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public interface IScoringService
{
    void Calculate(
        TournamentMatch match,
        IReadOnlyList<TournamentTeamMatchResult> teamResults,
        IReadOnlyList<TournamentTeam> teams,
        IReadOnlyList<TournamentPlayer> players);
}
