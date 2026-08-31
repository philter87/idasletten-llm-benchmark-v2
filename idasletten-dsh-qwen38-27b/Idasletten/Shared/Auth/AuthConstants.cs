namespace Idasletten.Auth;

public static class AuthConstants
{
    public const string AppScheme = "AppCookie";
    public const string AzureAdScheme = "AzureAd";
    public const string IdentityPolicy = "IdentityRequired";
}

/// <summary>Claims used for the app cookie (both Azure AD and test-user sign-ins).</summary>
public static class AppClaims
{
    public const string Username = "idasletten:username";
    public const string Name = "idasletten:name";
    public const string Email = "idasletten:email";
    public const string ImageUrl = "idasletten:image";
    public const string TestUser = "idasletten:testuser";
}
