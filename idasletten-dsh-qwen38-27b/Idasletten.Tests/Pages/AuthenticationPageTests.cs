using System.Net;
using Idasletten.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class AuthenticationPageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthenticationPageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = factory.Services.CreateScope();
    }

    private readonly Microsoft.Extensions.DependencyInjection.IServiceScope _scope;

    private AppDbContext Db => _scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Should_RedirectToLogin_When_AnonymousVisitsCreatePage()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Should_SignInTestUser_When_CredentialsAreValid()
    {
        // Arrange — no auto-redirect so the sign-in 302 target is inspectable
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var returnUrl = await client.LoginAsTestUserAsync();

        // Assert — landed on the return URL and the gated page now renders
        Assert.Equal("/tournaments", returnUrl);
        var createHtml = await client.GetStringAsync("/tournaments/create");
        Assert.Contains("Create tournament", createHtml);
    }

    [Fact]
    public async Task Should_NotSignIn_When_PasswordIsWrong()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginHtml = await client.GetStringAsync("/login");

        // Act
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = TestWebApplicationFactory.TestEmail,
            ["Password"] = "wrong-password",
            ["__RequestVerificationToken"] = loginHtml.Token()
        });
        var response = await client.PostAsync("/login?handler=TestLogin", form);

        // Assert — re-rendered login page with an error, not a sign-in redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body);
    }

    [Fact]
    public async Task Should_GateEditOfDoneMatch_When_AnonymousTriesToEdit()
    {
        // Arrange — find a done match in the seeded Valkyrior Open
        var client = _factory.CreateClient();
        var db = Db;
        var vo = await db.Tournaments.FirstAsync(t => t.Name == "Valkyrior Open");
        var match = await db.TournamentMatches
            .FirstAsync(m => m.TournamentId == vo.Id && m.State == Idasletten.Models.MatchState.Done);

        // Act — anonymous GETs always see the read-only result; an edit POST is the gated action
        var viewHtml = await client.GetStringAsync($"/tournaments/{vo.Id}/create-match?match={match.Id}");
        var noRedirect = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // The edit view renders a form (with an antiforgery token) even for anonymous users
        var editHtml = await noRedirect.GetStringAsync($"/tournaments/{vo.Id}/create-match?match={match.Id}&edit=true");
        var postForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Teams[0][Goals]"] = "5",
            ["Teams[1][Goals]"] = "5",
            ["MatchId"] = match.Id.ToString(),
            ["__RequestVerificationToken"] = editHtml.Token()
        });
        var postResponse = await noRedirect.PostAsync($"/tournaments/{vo.Id}/create-match", postForm);

        // Assert — read-only result with a login prompt; the edit view is a form, but the POST is bounced to /login
        Assert.Contains("Final result", viewHtml);
        Assert.Contains("Sign in to edit", viewHtml);
        Assert.DoesNotContain("Final result", editHtml);
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains("/login", postResponse.Headers.Location?.ToString());
    }
}
