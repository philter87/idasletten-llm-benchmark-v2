using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Idasletten.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("TestUser:Email", "test@idasletten.local");
        builder.UseSetting("TestUser:Password", "test-password");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdaslettenDbContext>>();
            services.RemoveAll<SqliteConnection>();
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            services.AddSingleton(connection);
            services.AddDbContext<IdaslettenDbContext>(options => options.UseSqlite(connection));
        });
    }
}
