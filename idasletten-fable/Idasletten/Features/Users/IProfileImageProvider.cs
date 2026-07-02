namespace Idasletten.Features.Users;

/// <summary>Resolves a profile image for a new user, e.g. via the Azure Graph API.</summary>
public interface IProfileImageProvider
{
    Task<string?> GetImageUrlAsync(string userName, string? email, CancellationToken ct = default);
}

/// <summary>
/// Fetches organisation profile photos via the Microsoft Graph API when configured
/// (Graph:TenantId, Graph:ClientId, Graph:ClientSecret). Returns null otherwise, so
/// user creation never depends on Graph being reachable.
/// </summary>
public class GraphProfileImageProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<GraphProfileImageProvider> logger)
    : IProfileImageProvider
{
    public async Task<string?> GetImageUrlAsync(string userName, string? email, CancellationToken ct = default)
    {
        var tenantId = configuration["Graph:TenantId"];
        var clientId = configuration["Graph:ClientId"];
        var clientSecret = configuration["Graph:ClientSecret"];
        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(email))
            return null;

        try
        {
            var http = httpClientFactory.CreateClient("graph");
            using var tokenResponse = await http.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                }), ct);
            if (!tokenResponse.IsSuccessStatusCode)
                return null;

            var token = (await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct))?.access_token;
            if (token is null)
                return null;

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(email)}/photo/$value");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var photoResponse = await http.SendAsync(request, ct);
            if (!photoResponse.IsSuccessStatusCode)
                return null;

            var bytes = await photoResponse.Content.ReadAsByteArrayAsync(ct);
            var contentType = photoResponse.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch profile image for {Email} from Graph", email);
            return null;
        }
    }

    private record TokenResponse(string access_token);
}
