using Idasletten.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Idasletten.Tests;

public class IdaslettenWebFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public IdaslettenWebFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration and replace with shared in-memory connection.
            var dbDescriptor = services.Single(d => d.ServiceType == typeof(IdaslettenDbContext));
            services.Remove(dbDescriptor);

            services.AddDbContext<IdaslettenDbContext>(opts => opts.UseSqlite(_connection));

            // Remove the SqliteConnectionHolder singleton (avoid double-open in-memory).
            var holder = services.FirstOrDefault(d => d.ServiceType == typeof(SqliteConnectionHolder));
            if (holder is not null) services.Remove(holder);

            // Provide a stub holder reusing our shared connection so Program.cs can still resolve it.
            services.AddSingleton(new SqliteConnectionHolder(_connection));
        });
        var host = base.CreateHost(builder);

        // Apply migrations + seed once on the shared connection.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        db.Database.Migrate();
        SeedData.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}