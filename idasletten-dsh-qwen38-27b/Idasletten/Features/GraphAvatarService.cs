using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Idasletten;

/// <summary>
/// Graph API client for user photos. Enabled when Graph:ClientId and
/// Graph:TokenScope (or the AzureAd app registration) are configured.
/// Stores the avatar as a base64 data URI so no static hosting is needed.
/// </summary>
public sealed class GraphAvatarService : IGraphAvatarService
{
    private readonly HttpClient _http;
    private readonly ITokenAcquirer _tokens;
    private readonly string? _scope;

    public GraphAvatarService(HttpClient http, ITokenAcquirer tokens, IConfiguration config, ILogger<GraphAvatarService> logger)
    {
        _http = http;
        _tokens = tokens;
        _scope = config["Graph:Scope"] ?? config["AzureAd:Scope"];
        _http.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
    }

    public bool IsConfigured => _tokens.CanAcquireTokens;

    public IGraphAvatarService AsGraphAvatarService() => this;

    public async Task<string?> GetAvatarUrlAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(_scope)) return null;
        try
        {
            var token = await _tokens.AcquireTokenAsync(new[] { _scope }, cancellationToken);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var bytes = await _http.GetByteArrayAsync($"users/{Uri.EscapeDataString(userPrincipalName)}/photo/$value", cancellationToken);
            if (bytes.Length == 0) return null;
            return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }
}
