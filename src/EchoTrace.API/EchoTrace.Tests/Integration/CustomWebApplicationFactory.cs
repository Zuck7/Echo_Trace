using EchoTrace.API;
using EchoTrace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EchoTrace.Tests.Integration;

/// <summary>
/// Swaps the SQL Server DbContext for a fresh, isolated EF Core InMemory database per factory
/// instance, so auth/RBAC/tenant-isolation flows can run over real HTTP without a live SQL Server
/// (which the CI runner doesn't provision — see docs/ROADMAP.md's TestContainers target for the
/// SQL-Server-specific path, e.g. the raw recursive-CTE supply-chain-tree query, not covered here).
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-long-test-key-for-jwt-signing-only",
                ["Jwt:Issuer"] = "echotrace-test",
                ["Jwt:Audience"] = "echotrace-test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Removing just DbContextOptions<T> isn't enough: AddDbContext also registers an
            // IDbContextOptionsConfiguration<T> delegate that still calls the original
            // UseSqlServer(...) from Program.cs, so both providers end up configured on the same
            // options builder ("only a single database provider can be registered"). Strip both.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<EchoTraceDbContext>)
                    || d.ServiceType == typeof(IDbContextOptionsConfiguration<EchoTraceDbContext>))
                .ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            services.AddDbContext<EchoTraceDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}
