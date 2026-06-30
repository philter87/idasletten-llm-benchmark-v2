using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Idasletten.Tests;

/// <summary>
/// Boots the real app in the Development environment so Program.cs's own startup logic
/// (in-memory SQLite, migrate, DbSeeder) runs unmodified - each factory instance gets its
/// own isolated, pre-seeded database.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserEmail = "factory-test-user@example.com";
    public const string TestUserPassword = "Fact0ryTestPassword!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("TestUser:Email", TestUserEmail);
        builder.UseSetting("TestUser:Password", TestUserPassword);
    }
}
