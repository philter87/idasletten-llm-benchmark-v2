namespace Idasletten.Features.Matches;

public enum SeedingType
{
    /// Teams chosen randomly.
    Random,

    /// Using the seed tournament's ranking, pair best with worst (1+N, 2+(N-1), ...).
    Equality,

    /// Split ranked players into a top half and bottom half, then pair the best of the top
    /// half with the best of the bottom half, and so on (10 players: 1+6, 2+7, 3+8, 4+9, 5+10).
    Fair
}
