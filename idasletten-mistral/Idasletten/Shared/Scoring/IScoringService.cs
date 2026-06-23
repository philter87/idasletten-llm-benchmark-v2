using Idasletten.Features.Tournaments;

namespace Idasletten.Shared.Scoring;

public interface IScoringService
{
    Task UpdatePlayerScoresAfterMatch(TournamentMatch match, Tournament tournament, CancellationToken cancellationToken = default);
    Task InitializePlayerScores(Tournament tournament, CancellationToken cancellationToken = default);
    double CalculateEloRatingChange(double winnerRating, double loserRating, bool winnerWon, int kFactor = 32);
    Task UpdateTrueSkillScores(TournamentMatch match, Tournament tournament, CancellationToken cancellationToken = default);
}
