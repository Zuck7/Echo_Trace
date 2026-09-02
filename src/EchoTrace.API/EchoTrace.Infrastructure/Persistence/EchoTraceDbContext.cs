using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EchoTrace.Infrastructure.Persistence;

public class EchoTraceDbContext(
    DbContextOptions<EchoTraceDbContext> options,
    ICurrentTenantService? currentTenant = null) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<SupplyChainEdge> SupplyChainEdges => Set<SupplyChainEdge>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EchoTraceDbContext).Assembly);

        // Global query filters — enforce tenant isolation.
        // IMPORTANT: reference `currentTenant.TenantId` directly in each filter lambda rather than
        // hoisting it into a local variable first. EF Core only calls OnModelCreating once (the
        // compiled model is cached across all DbContext instances/requests), so a captured local
        // would freeze the tenant id from whichever request happened to build the model first.
        // Referencing the injected service's property keeps it bound to *this* context instance,
        // so it's re-evaluated correctly on every request.
        if (currentTenant is not null)
        {
            modelBuilder.Entity<Organization>()
                .HasQueryFilter(o => o.TenantId == currentTenant.TenantId);

            modelBuilder.Entity<SupplyChainEdge>()
                .HasQueryFilter(e => e.TenantId == currentTenant.TenantId);

            modelBuilder.Entity<Document>()
                .HasQueryFilter(d => d.TenantId == currentTenant.TenantId);

            modelBuilder.Entity<User>()
                .HasQueryFilter(u => u.TenantId == currentTenant.TenantId);

            // AuditLogEntries intentionally NOT filtered — Platform Admins query across tenants
        }
    }
}
