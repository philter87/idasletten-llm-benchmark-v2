namespace Idasletten.Features.Users.Photos;

/// <summary>
/// Looks up a profile picture for a user in the organisation. The Graph implementation is used when
/// the app is configured with an Azure app registration, otherwise the no-op one is registered.
/// </summary>
public interface IUserPhotoProvider
{
    Task<string?> GetPhotoUrlAsync(string initials, string? email, CancellationToken cancellationToken);
}

/// <summary>Used locally, in tests and whenever Graph is not configured.</summary>
public class NoUserPhotoProvider : IUserPhotoProvider
{
    public Task<string?> GetPhotoUrlAsync(string initials, string? email, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);
}
