namespace Idasletten.Features.Scoring;

public static class ScoreDefaults
{
    /// <summary>Rating every player starts with in an Elo tournament.</summary>
    public const double EloStartRating = 1200;

    /// <summary>Elo K-factor - how much a single match can move the rating.</summary>
    public const double EloKFactor = 32;

    /// <summary>Lives every player starts with in a Lives tournament.</summary>
    public const int StartingLives = 3;

    /// <summary>TrueSkill mu (the library default, 25).</summary>
    public const double TrueSkillInitialMean = 25.0;

    /// <summary>TrueSkill sigma (the library default, 25/3).</summary>
    public const double TrueSkillInitialDeviation = 25.0 / 3.0;
}
