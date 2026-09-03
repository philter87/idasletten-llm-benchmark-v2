namespace Idasletten.Auth;

public sealed class TestUserOptions
{
    public const string Section = "TestUser";
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool Enabled => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
