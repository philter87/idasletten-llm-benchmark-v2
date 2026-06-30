namespace Idasletten.Shared.Auth;

/// <summary>
/// The test-only login (shown next to the Microsoft button) is enabled only when both
/// TestUser__Email and TestUser__Password env vars are set. The matching User row is seeded
/// by DbSeeder so the test user has real tournament history to look at.
/// </summary>
public record TestUserOptions(bool Enabled, string? Email, string? Password)
{
    public static TestUserOptions FromConfiguration(IConfiguration configuration)
    {
        var email = configuration["TestUser:Email"];
        var password = configuration["TestUser:Password"];
        var enabled = !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password);
        return new TestUserOptions(enabled, email, password);
    }
}
