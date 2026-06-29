using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data.Common;

namespace Idasletten.Tests.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(DbConnection));

            services.AddSingleton<DbConnection>(_connection);
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Features.Users.AppUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var email = config["TestUser__Email"] ?? "test@idasletten.local";
        var password = config["TestUser__Password"] ?? "Test1234!";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing == null)
        {
            await userManager.CreateAsync(new Features.Users.AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                Username = "TST",
                Name = "Test User",
                EmailConfirmed = true
            }, password);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
