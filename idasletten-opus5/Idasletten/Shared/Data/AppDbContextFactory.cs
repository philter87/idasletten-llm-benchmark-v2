using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Idasletten.Shared.Data;

/// <summary>
/// Used by "dotnet ef migrations add ..." so the CLI does not have to boot the whole web host.
/// The connection string is irrelevant for scaffolding - only the provider matters.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=idasletten-design-time.db")
            .Options;

        return new AppDbContext(options);
    }
}
