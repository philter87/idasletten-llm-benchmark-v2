namespace Idasletten.Shared.Domain;

public enum ScoreSystem
{
    Elo,
    TrueSkill,
    Lives,
    WinCount
}

public enum MatchState
{
    Planned,
    Done,
    Cancelled
}

public enum SeedingType
{
    Random,
    Equality,
    Fair
}
