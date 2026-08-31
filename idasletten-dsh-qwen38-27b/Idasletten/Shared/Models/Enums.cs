namespace Idasletten.Models;

/// <summary>How a tournament calculates <see cref="TournamentPlayer.Score"/>.</summary>
public enum ScoreSystem
{
    Elo = 0,
    TrueSkill = 1,
    Lives = 2,
    WinCount = 3
}

/// <summary>Lifecycle of a <see cref="TournamentMatch"/>.</summary>
public enum MatchState
{
    Planned = 0,
    Done = 1,
    Cancelled = 2
}

/// <summary>How "plan several matches" orders the pairings.</summary>
public enum SeedingType
{
    Random = 0,
    Equality = 1,
    Fair = 2
}
