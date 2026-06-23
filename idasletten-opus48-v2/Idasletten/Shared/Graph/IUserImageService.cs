namespace Idasletten.Shared.Graph;

/// <summary>Fetches a user's profile photo from the organisation directory (Azure Graph API).</summary>
public interface IUserImageService
{
    /// <summary>Returns an image URL (data URI) for the given username/email, or null if unavailable.</summary>
    Task<string?> GetImageUrlAsync(string? email, CancellationToken ct = default);
}

/// <summary>Fallback used when Graph is not configured (local/test). Always returns null.</summary>
public class NullUserImageService : IUserImageService
{
    public Task<string?> GetImageUrlAsync(string? email, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}
