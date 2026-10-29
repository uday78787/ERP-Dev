using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Erp.Infrastructure.Tests;

internal sealed class ErpApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=localhost;Database=ignored;Username=ignored;Password=not-a-secret";
    private const string TestBootstrapToken =
        "test-bootstrap-token-with-at-least-32-characters";
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string? priorConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Erp");
    private readonly string? priorBootstrapToken =
        Environment.GetEnvironmentVariable("Security__BootstrapToken");

    public ErpApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Erp", TestConnectionString);
        Environment.SetEnvironmentVariable("Security__BootstrapToken", TestBootstrapToken);
        connection.Open();
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Erp"] = TestConnectionString,
                ["Security:BootstrapToken"] = TestBootstrapToken,
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ErpDbContext>();
            services.RemoveAll<DbContextOptions<ErpDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ErpDbContext>>();
            services.AddSingleton(connection);
            services.AddDbContext<ErpDbContext>((provider, options) =>
                options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
            Environment.SetEnvironmentVariable("ConnectionStrings__Erp", priorConnectionString);
            Environment.SetEnvironmentVariable("Security__BootstrapToken", priorBootstrapToken);
        }
    }
}
