using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoTrace.Infrastructure.Persistence.Configurations;

public class SupplyChainEdgeConfiguration : IEntityTypeConfiguration<SupplyChainEdge>
{
    public void Configure(EntityTypeBuilder<SupplyChainEdge> builder)
    {
        builder.ToTable("SupplyChainEdges");
        builder.HasKey(e => e.EdgeId);
        builder.Property(e => e.EdgeId).ValueGeneratedNever();
        builder.Property(e => e.RelationshipType).IsRequired().HasMaxLength(30).HasDefaultValue("SOURCES");
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => new { e.ParentOrgId, e.IsActive })
            .HasDatabaseName("IX_Edges_ParentOrg")
            .IncludeProperties(e => new { e.ChildOrgId, e.TenantId });

        builder.HasIndex(e => new { e.ChildOrgId, e.IsActive })
            .HasDatabaseName("IX_Edges_ChildOrg")
            .IncludeProperties(e => new { e.ParentOrgId, e.TenantId });

        // Unique constraint: no duplicate active edges between the same org pair + material
        builder.HasIndex(e => new { e.ParentOrgId, e.ChildOrgId, e.MaterialId })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_Edges_ActivePair");
    }
}
