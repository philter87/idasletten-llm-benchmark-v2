using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Idasletten.Tests;

/// <summary>
/// Boots the app with its own uniquely named shared in-memory SQLite database,
/// so every factory gets isolated, migrated and seeded data (the same seeding
/// that runs when the app starts locally).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserEmail = "test@idasletten.local";
    public const string TestUserPassword = "ragnarok-test-2026";

    private readonly string _databaseName = $"test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default",
            $"Data Source={_databaseName};Mode=Memory;Cache=Shared");
        builder.UseSetting("TestUser:Email", TestUserEmail);
        builder.UseSetting("TestUser:Password", TestUserPassword);
    }
}
