using Azure.Identity;
using Microsoft.Graph;

namespace Idasletten.Shared.Graph;

/// <summary>
/// Fetches profile photos from Microsoft Graph using app (client-credentials) permissions.
/// Returns the photo as a data URI so it can be stored directly in User.ImageUrl.
/// </summary>
public class GraphUserImageService : IUserImageService
{
    private readonly GraphServiceClient _graph;
    private readonly ILogger<GraphUserImageService> _logger;

    public GraphUserImageService(GraphServiceClient graph, ILogger<GraphUserImageService> logger)
    {
        _graph = graph;
        _logger = logger;
    }

    public static GraphServiceClient CreateClient(string tenantId, string clientId, string clientSecret)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task<string?> GetImageUrlAsync(string? email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        try
        {
            await using var stream = await _graph.Users[email].Photo.Content.GetAsync(cancellationToken: ct);
            if (stream is null) return null;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();
            if (bytes.Length == 0) return null;
            return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch Graph photo for {Email}", email);
            return null;
        }
    }
}
