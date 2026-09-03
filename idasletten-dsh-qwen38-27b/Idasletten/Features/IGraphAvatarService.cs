namespace Idasletten;

/// <summary>Resolves a user's avatar via the Microsoft Graph API (best-effort).</summary>
public interface IGraphAvatarService
{
    bool IsConfigured { get; }
    Task<string?> GetAvatarUrlAsync(string userPrincipalName, CancellationToken cancellationToken = default);
}
