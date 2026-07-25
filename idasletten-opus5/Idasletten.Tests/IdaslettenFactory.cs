using System.Text.RegularExpressions;
using Idasletten.Shared.Data;
using Idasletten.Shared.Startup;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

/// <summary>
/// Boots the real application on its own SQLite in-memory database. Migrations and the seeding run
/// exactly as they do locally, so the tests work on the same data a developer sees.
/// </summary>
public class IdaslettenFactory : WebApplicationFactory<Program>
{
    public const string TestUserEmail = "test@idasletten.dk";
    public const string TestUserPassword = "Valhal123";
    public const string TestUserInitials = "TST";

    private readonly string databaseName = "IdaslettenTests-" + Guid.NewGuid().ToString("N");
    private readonly SemaphoreSlim initialiseGate = new(1, 1);
    private bool isInitialised;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Idasletten"] = InMemoryDatabase.ConnectionString(databaseName),
                // WebApplicationFactory boots more than one host over the same database, so the tests
                // migrate and seed once themselves (see InitialiseAsync) instead of on host start.
                ["Database:AutoInitialize"] = "false",
                ["Seed:Enabled"] = "true",
                // Both are needed for the test login to be enabled at all.
                ["TestUser:Email"] = TestUserEmail,
                ["TestUser:Password"] = TestUserPassword,
                ["TestUser:Initials"] = TestUserInitials,
            });
        });
    }

    /// <summary>
    /// Migrates and seeds this factory's database exactly once. Test classes call it from
    /// IAsyncLifetime.InitializeAsync, so every test starts on the fully seeded data.
    /// </summary>
    public async Task InitialiseAsync()
    {
        await initialiseGate.WaitAsync();
        try
        {
            if (isInitialised)
            {
                return;
            }

            await using var scope = Services.CreateAsyncScope();
            await DatabaseSetup.RunAsync(scope.ServiceProvider, seed: true);
            isInitialised = true;
        }
        finally
        {
            initialiseGate.Release();
        }
    }

    /// <summary>Sends a command or query through MediatR, just like a page would.</summary>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>Runs an assertion straight against the database.</summary>
    public async Task<TResult> QueryAsync<TResult>(Func<AppDbContext, Task<TResult>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>A client that keeps cookies, so it can log in and stay logged in.</summary>
    public HttpClient CreateWebClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    /// <summary>Logs in with the test user and returns the client holding the authentication cookie.</summary>
    public async Task<HttpClient> CreateLoggedInClientAsync()
    {
        var client = CreateWebClient();

        var loginPage = await client.GetStringAsync("/login");
        var response = await client.PostAsync("/login?handler=TestUser", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = TestUserEmail,
                ["Password"] = TestUserPassword,
                ["__RequestVerificationToken"] = AntiforgeryToken(loginPage),
            }));

        if (response.StatusCode is not (System.Net.HttpStatusCode.Redirect or System.Net.HttpStatusCode.Found))
        {
            throw new InvalidOperationException($"Test login failed with {response.StatusCode}");
        }

        return client;
    }

    public static string AntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html, """<input name="__RequestVerificationToken" type="hidden" value="([^"]+)" />""");

        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
