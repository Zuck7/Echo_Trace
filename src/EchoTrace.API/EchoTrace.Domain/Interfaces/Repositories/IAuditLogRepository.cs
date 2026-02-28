using EchoTrace.Domain.Entities;

namespace EchoTrace.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<AuditLogEntry?> GetLastEntryForEntityAsync(string entityType, string entityId, CancellationToken ct = default);
    Task<List<AuditLogEntry>> GetEntriesForEntityAsync(string entityType, string entityId, CancellationToken ct = default);
    Task<List<AuditLogEntry>> QueryAsync(Guid? tenantId, string? entityType, string? entityId, string? action, DateTime? from, DateTime? to, int pageSize, string? cursor, CancellationToken ct = default);
}
