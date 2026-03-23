using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoTrace.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");
        builder.HasKey(a => a.AuditId);
        builder.Property(a => a.AuditId).ValueGeneratedOnAdd();
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(20);
        builder.Property(a => a.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.PerformedByIpAddress).HasMaxLength(45);
        builder.Property(a => a.ChainHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(a => a.OccurredAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAt })
            .HasDatabaseName("IX_AuditLog_Entity");
    }
}
