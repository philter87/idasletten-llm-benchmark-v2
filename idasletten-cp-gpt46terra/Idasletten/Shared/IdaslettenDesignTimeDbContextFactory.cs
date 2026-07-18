using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Idasletten.Shared;

public class IdaslettenDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdaslettenDbContext>
{
    public IdaslettenDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdaslettenDbContext>()
            .UseSqlite("Data Source=idasletten-design.db")
            .Options;
        return new IdaslettenDbContext(options);
    }
}
