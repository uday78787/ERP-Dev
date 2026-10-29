using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Erp.Infrastructure.Persistence;

public sealed class ErpDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Erp");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Erp before running Entity Framework migration commands.");
        }

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ErpDbContext(options);
    }
}
