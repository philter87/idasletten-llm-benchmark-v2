using Bogus;

namespace Idasletten.Tests.TestData;

/// <summary>
/// Static "Any" factory: randomised-helpers to populate all the fields of an entity.
/// Mirrors the spec's "static `Any` class with methods like Any.User()".
/// </summary>
public static class Any
{
    private static readonly Faker _f = new Faker();

    public static string Initials() => _f.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ").ToUpperInvariant();
    public static string Name() => _f.Name.FullName();
    public static string TournamentName() => $"{_f.PickRandom("Ragnarok", "Valkyrie", "Bifrost", "Mjølner", "Yggdrasil")} {_f.Random.Int(1, 9)}";
    public static int Goals() => _f.Random.Int(0, 5);
    public static int TeamSize() => _f.Random.Int(1, 2);
    public static int PointsToWin() => _f.Random.Int(3, 10);
}