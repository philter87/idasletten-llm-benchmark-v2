using Idasletten.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

/// <summary>
/// Boots the real application (top-level Program) with an in-memory SQLite
/// database and seed data, mirroring how local Development runs are seeded.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestEmail = "test@idasletten.dk";
    public const string TestPassword = "Correct-Horse-42";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Mode"] = "Memory",
                ["Seed:Enabled"] = "true",
                ["TestUser:Email"] = TestEmail,
                ["TestUser:Password"] = TestPassword,
                ["AzureAd:ClientId"] = "",
                ["AzureAd:ClientSecret"] = null,
                ["AzureAd:TenantId"] = null
            });
        });
    }
}
