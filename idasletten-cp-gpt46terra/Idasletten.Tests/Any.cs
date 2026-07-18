using Idasletten.Shared;

namespace Idasletten.Tests;

public static class Any
{
    public static User User() => new()
    {
        Username = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
        Name = $"Viking {Guid.NewGuid():N}"[..14]
    };
}
