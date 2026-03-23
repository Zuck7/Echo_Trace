using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoTrace.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.OrgId);
        builder.Property(o => o.OrgId).ValueGeneratedNever();
        builder.Property(o => o.LegalName).IsRequired().HasMaxLength(300);
        builder.Property(o => o.Country).IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(o => o.ContactEmail).IsRequired().HasMaxLength(320);
        builder.Property(o => o.IndustrySector).HasMaxLength(100);
        builder.Property(o => o.RegistrationNumber).HasMaxLength(100);
        builder.Property(o => o.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(o => o.Address).HasColumnType("nvarchar(max)");
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(o => o.TenantId).HasDatabaseName("IX_Organizations_TenantID");
    }
}
