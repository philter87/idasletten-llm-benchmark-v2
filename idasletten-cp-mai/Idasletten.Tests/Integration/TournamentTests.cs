using FluentAssertions;
using Idasletten.Tests.Factories;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Idasletten.Tests.Integration;

public class TournamentTests : IntegrationTestBase
{
    public TournamentTests(CustomWebApplicationFactory factory) : base(factory)
    {
        Factory.SeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Should_ReturnHomePage_When_Anonymous()
    {
        var response = await Client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_AnonymousUserCreatesTournament()
    {
        var response = await Client.GetAsync("/tournaments/create");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/login");
    }

    [Fact]
    public async Task Should_CreateTournament_When_Authenticated()
    {
        await LoginAsync();

        var createPage = await Client.GetAsync("/tournaments/create");
        createPage.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = ExtractToken(await createPage.Content.ReadAsStringAsync());
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", "Integration Test Tournament" },
            { "TeamSize", "2" },
            { "PointsToWin", "5" },
            { "ScoreSystem", "Elo" },
            { "IsPublic", "true" },
            { "__RequestVerificationToken", token }
        });

        var response = await Client.PostAsync("/tournaments/create?action=create", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Should_RecordMatch_When_Anonymous()
    {
        await LoginAsync();

        // Create tournament
        var createPage = await Client.GetAsync("/tournaments/create");
        var token = ExtractToken(await createPage.Content.ReadAsStringAsync());
        var createContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", "Match Test" },
            { "TeamSize", "2" },
            { "PointsToWin", "5" },
            { "ScoreSystem", "Elo" },
            { "IsPublic", "true" },
            { "__RequestVerificationToken", token }
        });
        var createResponse = await Client.PostAsync("/tournaments/create?action=create", createContent);
        var location = createResponse.Headers.Location!;
        var path = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
        var tournamentId = path.Trim('/').Split('/').Last();
        var matchUrl = $"/tournaments/{tournamentId}/create-match";

        // Use a fresh anonymous client to prove login is not required
        var anonymousClient = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var matchPage = await anonymousClient.GetAsync(matchUrl);
        matchPage.StatusCode.Should().Be(HttpStatusCode.OK);

        var matchToken = ExtractToken(await matchPage.Content.ReadAsStringAsync());
        var matchContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Teams[0].Initials", "ODN, THO" },
            { "Teams[0].Score", "5" },
            { "Teams[1].Initials", "KLA, BJA" },
            { "Teams[1].Score", "3" },
            { "__RequestVerificationToken", matchToken }
        });

        var response = await anonymousClient.PostAsync(matchUrl, matchContent);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
