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

        // Global query filters — enforce tenant isolation
        if (currentTenant is not null)
        {
            var tenantId = currentTenant.TenantId;

            modelBuilder.Entity<Organization>()
                .HasQueryFilter(o => o.TenantId == tenantId);

            modelBuilder.Entity<SupplyChainEdge>()
                .HasQueryFilter(e => e.TenantId == tenantId);

            modelBuilder.Entity<Document>()
                .HasQueryFilter(d => d.TenantId == tenantId);

            modelBuilder.Entity<User>()
                .HasQueryFilter(u => u.TenantId == tenantId);

            // AuditLogEntries intentionally NOT filtered — Platform Admins query across tenants
        }
    }
}
