using System.Net;
using Idasletten.Tests.TestSupport;

namespace Idasletten.Tests.Pages;

public class PageAccessTests(IdaslettenWebApplicationFactory factory) : IClassFixture<IdaslettenWebApplicationFactory>
{
    [Fact]
    public async Task Should_ReturnOk_When_RequestingHomePageAnonymously()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_RequestingAllTournamentsAnonymously()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/tournaments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_RequestingCreateTournamentAnonymously()
    {
        // Arrange
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TournamentDoesNotExist()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/tournaments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
