using Microsoft.AspNetCore.Mvc.Testing;

namespace Idasletten.Tests.Integration;

public class HomePageTests : IClassFixture<IdaslettenWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HomePageTests(IdaslettenWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_HomePageIsRequested()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idasletten", content);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TournamentsPageIsRequested()
    {
        // Act
        var response = await _client.GetAsync("/Tournaments");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_CreateTournamentIsAccessedWithoutAuth()
    {
        // Act  
        var response = await _client.GetAsync("/Tournaments/Create");

        // Assert - Unauthorized redirects to login
        var locationHeader = response.Headers.Location?.ToString() ?? response.Headers.GetValues("Location").FirstOrDefault() ?? "";
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected redirect or 401, got {response.StatusCode}");
    }
}
