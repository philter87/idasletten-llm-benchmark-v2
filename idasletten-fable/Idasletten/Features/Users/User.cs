namespace Idasletten.Features.Users;

/// <summary>
/// Mirrors the standard ASP.NET Core IdentityUser fields where they make sense,
/// plus the app-specific Name and ImageUrl.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Usually 3 initials. Unique.</summary>
    public string UserName { get; set; } = "";
    public string NormalizedUserName { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Optional — a user may not have an email address.</summary>
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }

    /// <summary>Fetched via the Azure Graph API when the user is created.</summary>
    public string? ImageUrl { get; set; }

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
}
