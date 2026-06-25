using Bogus;

namespace Idasletten.Tests;

public static class Any
{
    private static readonly Faker _faker = new();

    public static string Username() => _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    public static string Name() => _faker.Name.FullName();
    public static string Email() => _faker.Internet.Email();
    public static Guid Guid() => System.Guid.NewGuid();
    public static int Int(int min = 1, int max = 100) => _faker.Random.Int(min, max);
    public static string TournamentName() => _faker.Commerce.ProductName() + " Tournament";
}
