using Idasletten.Shared.Data;

namespace Idasletten.Shared.Startup;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Migrations are applied automatically on startup - there is no separate deployment step for the
    /// database - and an empty database is filled with seed data afterwards. See
    /// <see cref="DatabaseInitializer"/> for the work itself.
    /// </summary>
    public static IServiceCollection AddDatabaseInitialisation(this IServiceCollection services)
    {
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
