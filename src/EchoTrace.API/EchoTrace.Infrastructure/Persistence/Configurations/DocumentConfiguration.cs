using EchoTrace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoTrace.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.DocumentId);
        builder.Property(d => d.DocumentId).ValueGeneratedNever();
        builder.Property(d => d.DocumentType).IsRequired().HasMaxLength(50);
        builder.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(d => d.BlobPath).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.ContentHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(d => d.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
        builder.Property(d => d.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(d => new { d.OrgId, d.Status }).HasDatabaseName("IX_Documents_OrgID");
    }
}
