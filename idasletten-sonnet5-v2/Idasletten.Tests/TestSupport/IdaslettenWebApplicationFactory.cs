using Idasletten.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Idasletten.Tests.TestSupport;

/// <summary>
/// Custom WebApplicationFactory backed by a SQLite in-memory database, matching production's
/// SQLite provider (rather than EF's InMemory provider) so relational behavior — constraints,
/// LINQ translation, migrations — is exercised the same way in tests as it is for real.
/// Program.cs's own startup code applies migrations and seeds data against this same database.
/// </summary>
public class IdaslettenWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdaslettenDbContext>>();
            services.AddDbContext<IdaslettenDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
