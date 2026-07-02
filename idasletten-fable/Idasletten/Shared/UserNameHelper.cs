namespace Idasletten.Shared;

public static class UserNameHelper
{
    /// <summary>Derives 3-letter initials from a display name, falling back to the email local part.</summary>
    public static string InitialsFrom(string name, string? email)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var initials = parts.Length >= 2
            ? string.Concat(parts.Take(3).Select(p => char.ToUpperInvariant(p[0])))
            : new string(name.Where(char.IsLetter).Take(3).ToArray()).ToUpperInvariant();

        if (initials.Length >= 2)
            return initials;

        var localPart = email?.Split('@')[0] ?? name;
        return new string(localPart.Where(char.IsLetter).Take(3).ToArray()).ToUpperInvariant();
    }
}
