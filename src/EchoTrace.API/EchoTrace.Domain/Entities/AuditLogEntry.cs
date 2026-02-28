namespace EchoTrace.Domain.Entities;

public class AuditLogEntry
{
    public long AuditId { get; private set; }
    public Guid TenantId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid? PerformedByUserId { get; private set; }
    public string? PerformedByIpAddress { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string ChainHash { get; private set; } = string.Empty;

    private AuditLogEntry() { }

    public static AuditLogEntry Create(
        Guid tenantId,
        string entityType,
        string entityId,
        string action,
        string? oldValue,
        string? newValue,
        Guid? performedByUserId,
        string? ipAddress,
        string chainHash)
    {
        return new AuditLogEntry
        {
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            PerformedByUserId = performedByUserId,
            PerformedByIpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow,
            ChainHash = chainHash
        };
    }
}
