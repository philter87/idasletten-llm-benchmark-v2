namespace Idasletten.Shared.Infrastructure;

public static class DbContextExtensions
{
    public static async Task AutoMigrateAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
}
