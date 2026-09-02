using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoTrace.Infrastructure.Persistence;

/// <summary>
/// Seeds a single PLATFORM_ADMIN account on first startup so there's a way into the system
/// before any tenant self-registers. Controlled by Seed:AdminEmail / Seed:AdminPassword
/// (falls back to a well-known dev-only default so `docker compose up` is demoable out of the box).
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(
        EchoTraceDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var alreadySeeded = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Role == Roles.PlatformAdmin, ct);
        if (alreadySeeded) return;

        var email = configuration["Seed:AdminEmail"] ?? "admin@echotrace.dev";
        var password = configuration["Seed:AdminPassword"] ?? "ChangeMe123!";

        var tenant = Tenant.Create("Echo-Trace Platform");
        db.Tenants.Add(tenant);

        var admin = User.Create(tenant.TenantId, Guid.Empty, email, passwordHasher.Hash(password), Roles.PlatformAdmin);
        db.Users.Add(admin);

        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "Seeded PLATFORM_ADMIN {Email} — change this password before any non-local deployment",
            email);
    }
}
