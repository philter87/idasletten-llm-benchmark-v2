using Idasletten.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class IdaslettenWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"idasletten-test-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<IdaslettenDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<IdaslettenDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }
}
