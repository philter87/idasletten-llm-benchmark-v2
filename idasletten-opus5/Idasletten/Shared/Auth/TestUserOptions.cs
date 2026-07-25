namespace Idasletten.Shared.Auth;

/// <summary>
/// The test-only login shown next to the Microsoft button. It is only enabled when both
/// TestUser__Email and TestUser__Password are set as environment variables, so it can never be
/// switched on by accident in production.
/// </summary>
public class TestUserOptions
{
    public const string SectionName = "TestUser";

    public string? Email { get; set; }

    public string? Password { get; set; }

    /// <summary>Initials of the seeded test user.</summary>
    public string Initials { get; set; } = "TST";

    public string Name { get; set; } = "Test Viking";

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    public bool Matches(string? email, string? password) =>
        IsEnabled &&
        string.Equals(Email, email?.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Password, password, StringComparison.Ordinal);
}
