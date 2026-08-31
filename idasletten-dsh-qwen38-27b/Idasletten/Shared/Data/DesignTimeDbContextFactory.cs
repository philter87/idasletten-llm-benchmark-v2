using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Idasletten.Data;

/// <summary>Used by the dotnet-ef CLI only (file-based dev database).</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=idasletten-design.db")
            .Options;
        return new AppDbContext(options);
    }
}
