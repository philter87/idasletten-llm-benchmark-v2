using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;

namespace Idasletten;

public sealed class TokenAcquirer : ITokenAcquirer
{
    private readonly ITokenAcquisition? _tokenAcquisition;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public TokenAcquirer(ITokenAcquisition? tokenAcquisition, IConfiguration config, IHttpClientFactory httpFactory)
    {
        _tokenAcquisition = tokenAcquisition;
        _config = config;
        _http = httpFactory.CreateClient();
    }

    public bool CanAcquireTokens => _tokenAcquisition is not null;

    public async Task<string?> AcquireTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
    {
        if (_tokenAcquisition is not null)
            return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

        // Fallback: client credentials via the app registration's own client id/secret.
        var clientId = _config["AzureAd:ClientId"];
        var clientSecret = _config["AzureAd:ClientSecret"];
        var tenantId = _config["AzureAd:TenantId"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            return null;

        var form = new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = string.Join(' ', scopes)
        });
        using var resp = await _http.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", form, cancellationToken);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.TryGetProperty("access_token", out var t) ? t.GetString() : null;
    }
}
