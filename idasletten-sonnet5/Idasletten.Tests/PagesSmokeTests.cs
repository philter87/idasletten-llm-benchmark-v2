using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Idasletten.Tests;

public class PagesSmokeTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public void Should_HaveAnAntiforgeryTrigger_When_APageHasASelfPostingForm()
    {
        // Arrange: <form method="post"> only gets its antiforgery token auto-injected when
        // the FormTagHelper actually processes the tag, which requires an asp-* attribute
        // (asp-page, asp-page-handler, asp-action, or an explicit asp-antiforgery="true").
        // A form with none of those silently posts with no token and 400s - this happened
        // for real on the navbar logout form and the create-tournament/create-match forms.
        var pagesDirectory = Path.Combine(AppContext.BaseDirectory, "../../../../Idasletten/Pages");
        var formOpenTag = new Regex("""<form\s+method="post"[^>]*>""", RegexOptions.IgnoreCase);
        var hasAntiforgeryTrigger = new Regex("""asp-(page|action|antiforgery)""", RegexOptions.IgnoreCase);
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(pagesDirectory, "*.cshtml", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (Match match in formOpenTag.Matches(content))
            {
                if (!hasAntiforgeryTrigger.IsMatch(match.Value))
                {
                    offenders.Add($"{Path.GetFileName(file)}: {match.Value}");
                }
            }
        }

        // Assert
        Assert.True(offenders.Count == 0, "Forms missing an antiforgery trigger:\n" + string.Join("\n", offenders));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/tournaments")]
    [InlineData("/login")]
    public async Task Should_ReturnOk_When_RequestingPublicPage(string path)
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(path);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_RequestingCreateTournamentAnonymously()
    {
        // Arrange: creating a tournament requires login.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_RequestingTournamentDetailForUnknownId()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/tournaments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_RedirectWithoutError_When_PostingLogoutFormWithItsRenderedToken()
    {
        // Arrange: a real round-trip through the self-posting /logout form, the way a
        // browser actually submits it (including the antiforgery cookie + hidden field).
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var getResponse = await client.GetAsync("/logout");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = Regex.Match(html, """name="__RequestVerificationToken" type="hidden" value="([^"]+)""").Groups[1].Value;
        Assert.NotEmpty(token);

        // Act
        var postResponse = await client.PostAsync("/logout",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
    }
}
