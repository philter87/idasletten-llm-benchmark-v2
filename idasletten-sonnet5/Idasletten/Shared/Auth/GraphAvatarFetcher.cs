namespace Idasletten.Shared.Auth;

/// <summary>
/// Best-effort fetch of a user's photo from the Microsoft Graph API (delegated User.Read),
/// called when a User is first created via Azure AD login. Never throws: a missing/invalid
/// token or Graph being unreachable just means no avatar, not a failed login. Not exercised
/// by the test-user login (no real Graph token is available there).
/// </summary>
public class GraphAvatarFetcher(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment, ILogger<GraphAvatarFetcher> logger)
{
    public async Task<string?> TryFetchAndSaveAsync(string accessToken, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

            using var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var avatarsDir = Path.Combine(environment.WebRootPath, "avatars");
            Directory.CreateDirectory(avatarsDir);

            var fileName = $"{userId}.jpg";
            var filePath = Path.Combine(avatarsDir, fileName);
            await using (var fileStream = File.Create(filePath))
            {
                await response.Content.CopyToAsync(fileStream, cancellationToken);
            }

            return $"/avatars/{fileName}";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch Graph avatar for user {UserId}", userId);
            return null;
        }
    }
}
