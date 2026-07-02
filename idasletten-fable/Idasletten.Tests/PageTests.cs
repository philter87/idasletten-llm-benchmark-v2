using System.Net;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class PageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PageTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_ShowHeroTextAndPublicTournaments_When_HomePageIsRequested()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idasletten (Iðavöllr)", html);
        Assert.Contains("Ragnarok Forår 2026", html);
        Assert.DoesNotContain("Einherjernes Kamp", html); // private tournament stays off the home page
    }

    [Fact]
    public async Task Should_ListArchivedTournaments_When_TournamentsPageIsRequested()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/tournaments");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Valhal Høst 2025", html);
        Assert.Contains("Einherjernes Kamp", html);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_CreateTournamentPageIsRequestedAnonymously()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.StartsWith("http://localhost/login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Should_AllowCreateMatchPage_When_UserIsNotLoggedIn()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tournaments = await mediator.Send(new GetTournamentsQuery(PublicOnly: true));
        var tournament = tournaments[0].Tournament;

        // Act
        var response = await client.GetAsync($"/tournaments/{tournament.Id}/create-match");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Registrér kamp", html);
    }

    [Fact]
    public async Task Should_ShowScoreboardAndMatchCards_When_TournamentDetailIsRequested()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tournaments = await mediator.Send(new GetTournamentsQuery(PublicOnly: true));
        var tournament = tournaments.Single(t => t.Tournament.Name == "Ragnarok Forår 2026").Tournament;

        // Act
        var response = await client.GetAsync($"/tournaments/{tournament.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Stillingen", html);
        Assert.Contains("Næste kampe", html);
        Assert.Contains("Seneste resultater", html);
        Assert.Contains("THO", html);
    }

    [Fact]
    public async Task Should_ShowTestLoginOption_When_TestUserEnvVarsAreSet()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("testbruger", html);
    }

    [Fact]
    public async Task Should_Return404_When_TournamentDoesNotExist()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/tournaments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
