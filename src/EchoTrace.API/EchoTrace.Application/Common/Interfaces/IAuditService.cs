namespace EchoTrace.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string entityType,
        string entityId,
        string action,
        object? oldValue,
        object? newValue,
        CancellationToken ct = default);
}
