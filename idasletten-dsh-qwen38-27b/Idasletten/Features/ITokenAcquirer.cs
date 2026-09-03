namespace Idasletten;

/// <summary>Acquires client credentials for the Graph API when Azure AD is configured.</summary>
public interface ITokenAcquirer
{
    bool CanAcquireTokens { get; }
    Task<string?> AcquireTokenAsync(string[] scopes, CancellationToken cancellationToken = default);
}
