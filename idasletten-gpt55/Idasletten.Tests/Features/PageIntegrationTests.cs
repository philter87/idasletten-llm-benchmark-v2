using Idasletten.Tests.Infrastructure;

namespace Idasletten.Tests.Features;

public class PageIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PageIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_RenderHome_When_PublicTournamentsExist()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, html);
        Assert.Contains("Idasletten", html);
        Assert.Contains("Ragnarok Friday", html);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_CreateTournamentIsAnonymous()
    {
        // Arrange
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());
    }
}
