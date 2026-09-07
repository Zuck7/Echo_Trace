using System.Globalization;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EchoTrace.Infrastructure.Persistence.Repositories;

public class DocumentRepository(EchoTraceDbContext db) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
        db.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, ct);

    public Task<List<Document>> GetByOrgIdAsync(Guid orgId, CancellationToken ct = default) =>
        db.Documents.Where(d => d.OrgId == orgId).OrderByDescending(d => d.UploadedAt).ToListAsync(ct);

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Update(document);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<Document>> GetExpiringBeforeAsync(DateOnly cutoff, CancellationToken ct = default) =>
        db.Documents
            .Where(d => d.Status == "Active" && d.ExpiresAt != null && d.ExpiresAt <= cutoff)
            .ToListAsync(ct);

    public async Task<List<Document>> QueryAsync(
        Guid orgId, string? documentType, string? status, int pageSize, string? cursor, CancellationToken ct = default)
    {
        var query = db.Documents.Where(d => d.OrgId == orgId);

        if (!string.IsNullOrWhiteSpace(documentType)) query = query.Where(d => d.DocumentType == documentType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);

        if (DateTime.TryParse(cursor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var cursorDate))
            query = query.Where(d => d.UploadedAt < cursorDate);

        return await query
            .OrderByDescending(d => d.UploadedAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
