using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Scoring;

public interface IScoreCalculator
{
    double InitialScore { get; }
    void ApplyMatch(Tournament tournament, Dictionary<Guid, TournamentPlayer> playersByUserId, TournamentMatch match);
}
