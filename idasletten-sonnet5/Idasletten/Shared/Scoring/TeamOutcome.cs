using Idasletten.Features.TournamentPlayers;

namespace Idasletten.Shared.Scoring;

public record TeamOutcome(Guid TeamId, int GoalsWon, int GoalsLost, IReadOnlyList<TournamentPlayer> Players)
{
    public int NetGoals => GoalsWon - GoalsLost;
}
