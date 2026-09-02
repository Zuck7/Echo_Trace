using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoTrace.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.TenantId);
        builder.Property(t => t.TenantId).ValueGeneratedNever();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(300);
        builder.Property(t => t.PlanTier).IsRequired().HasMaxLength(20).HasDefaultValue("STANDARD");
        builder.Property(t => t.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
