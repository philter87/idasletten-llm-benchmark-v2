using System.Net;
using Xunit;

namespace Idasletten.Tests;

public class SmokeTests : IClassFixture<IdaslettenFactory>
{
    private readonly IdaslettenFactory _factory;
    public SmokeTests(IdaslettenFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/tournaments")]
    [InlineData("/login")]
    public async Task Should_Return200_When_PublicPageRequested(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_CreateTournamentRequestedAnonymously()
    {
        // Arrange — don't follow redirects so we can observe the challenge.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert — anonymous users are bounced (redirect) rather than served the page.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
