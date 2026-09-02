using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EchoTrace.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(EchoTraceDbContext db) : IAuditLogRepository
{
    public async Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        db.AuditLogEntries.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public Task<AuditLogEntry?> GetLastEntryForEntityAsync(string entityType, string entityId, CancellationToken ct = default) =>
        db.AuditLogEntries
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.AuditId)
            .FirstOrDefaultAsync(ct);

    public Task<List<AuditLogEntry>> GetEntriesForEntityAsync(string entityType, string entityId, CancellationToken ct = default) =>
        db.AuditLogEntries
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.OccurredAt)
            .ToListAsync(ct);

    public async Task<List<AuditLogEntry>> QueryAsync(
        Guid? tenantId, string? entityType, string? entityId, string? action,
        DateTime? from, DateTime? to, int pageSize, string? cursor, CancellationToken ct = default)
    {
        var query = db.AuditLogEntries.AsQueryable();

        if (tenantId.HasValue) query = query.Where(a => a.TenantId == tenantId.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value);

        if (long.TryParse(cursor, out var cursorId))
            query = query.Where(a => a.AuditId < cursorId);

        return await query
            .OrderByDescending(a => a.AuditId)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
