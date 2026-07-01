namespace Idasletten.Shared.Auth;

/// <summary>
/// Bound from the "TestUser" configuration section (env vars TestUser__Email / TestUser__Password).
/// The test-login option on /login is only shown when both are set.
/// </summary>
public class TestUserOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
