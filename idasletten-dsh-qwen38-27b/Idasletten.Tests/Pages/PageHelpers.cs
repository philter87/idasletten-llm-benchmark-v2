using System.Text.RegularExpressions;

namespace Idasletten.Tests;

/// <summary>Shared helpers for page-level (TestServer) tests.</summary>
public static class PageHelpers
{
    /// <summary>Extracts the antiforgery token from a rendered form page.</summary>
    public static string Token(this string html) =>
        Regex.Match(html, @"__RequestVerificationToken[^>]*value=""([^""]+)""").Groups[1].Value;

    /// <summary>Performs the test-user login and returns the landing URL.</summary>
    public static async Task<string> LoginAsTestUserAsync(this HttpClient client)
    {
        var loginHtml = await client.GetStringAsync("/login");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = TestWebApplicationFactory.TestEmail,
            ["Password"] = TestWebApplicationFactory.TestPassword,
            ["ReturnUrl"] = "/tournaments",
            ["__RequestVerificationToken"] = loginHtml.Token()
        });
        var response = await client.PostAsync("/login?handler=TestLogin", form);
        Assert.Equal(System.Net.HttpStatusCode.Found, response.StatusCode);
        return response.Headers.Location?.ToString() ?? "/";
    }
}
