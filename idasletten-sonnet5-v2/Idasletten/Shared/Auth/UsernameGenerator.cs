using Idasletten.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Auth;

public static class UsernameGenerator
{
    /// <summary>Derives 3-letter initials from a display name, e.g. "Ida Sletten" -> "IS", "Ida Marie Sletten" -> "IMS".</summary>
    public static string DeriveInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
        if (initials.Length > 3)
        {
            initials = string.Concat(initials[0], initials[^2], initials[^1]);
        }
        if (initials.Length == 0)
        {
            initials = "USR";
        }
        return initials;
    }

    /// <summary>Ensures the given username is unique, appending a numeric suffix if needed.</summary>
    public static async Task<string> EnsureUniqueAsync(IdaslettenDbContext db, string candidate, CancellationToken cancellationToken = default)
    {
        var normalized = candidate.ToUpperInvariant();
        var suffix = 1;
        var result = candidate;
        while (await db.Users.AnyAsync(u => u.NormalizedUserName == normalized, cancellationToken))
        {
            suffix++;
            result = $"{candidate}{suffix}";
            normalized = result.ToUpperInvariant();
        }
        return result;
    }
}
