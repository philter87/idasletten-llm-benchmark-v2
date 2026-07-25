using Idasletten.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Startup;

/// <summary>
/// Applies the migrations and seeds the database while the host starts. It is a hosted service and not
/// a few lines at the end of Program.cs, so the work belongs to the host lifetime and is finished
/// before the first request is served.
/// The tests turn it off with Database:AutoInitialize=false and call <see cref="DatabaseSetup"/>
/// themselves, because WebApplicationFactory boots more than one host over the same database.
/// </summary>
public class DatabaseInitializer(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Database:AutoInitialize", true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();

        // Seeding is on by default for the in-memory database (local development and tests) and off
        // for a real file database, so a deployment never gets demo tournaments by accident.
        var seedsByDefault = InMemoryDatabase.IsInMemory(
            InMemoryDatabase.ResolveConnectionString(configuration));

        await DatabaseSetup.RunAsync(
            scope.ServiceProvider,
            seed: configuration.GetValue("Seed:Enabled", seedsByDefault),
            cancellationToken);

        logger.LogInformation("Database is migrated and ready");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Migrating and seeding - shared by the hosted service and the test factory.</summary>
public static class DatabaseSetup
{
    public static async Task RunAsync(
        IServiceProvider scopedServices, bool seed, CancellationToken cancellationToken = default)
    {
        // Keeps the in-memory database alive for the lifetime of the app (no-op when using a file).
        scopedServices.GetRequiredService<InMemoryDatabaseKeepAlive>().Open();

        var db = scopedServices.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        if (seed)
        {
            await scopedServices.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
        }
    }
}
