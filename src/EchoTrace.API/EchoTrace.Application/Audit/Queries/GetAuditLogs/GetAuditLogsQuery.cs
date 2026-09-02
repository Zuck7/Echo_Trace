using MediatR;

namespace EchoTrace.Application.Audit.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    string? EntityType,
    string? EntityId,
    string? Action,
    DateTime? From,
    DateTime? To,
    int PageSize = 50,
    string? Cursor = null) : IRequest<List<AuditLogEntryDto>>;

public record AuditLogEntryDto(
    long AuditId,
    Guid TenantId,
    string EntityType,
    string EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    Guid? PerformedByUserId,
    string? PerformedByIpAddress,
    DateTime OccurredAt,
    string ChainHash);
