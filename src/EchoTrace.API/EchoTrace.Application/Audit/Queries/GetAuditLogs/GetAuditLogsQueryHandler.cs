using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Audit.Queries.GetAuditLogs;

/// <summary>Platform Admins see all tenants' entries; everyone else is scoped to their own tenant.</summary>
public sealed class GetAuditLogsQueryHandler(
    IAuditLogRepository auditRepo,
    ICurrentTenantService tenant) : IRequestHandler<GetAuditLogsQuery, List<AuditLogEntryDto>>
{
    public async Task<List<AuditLogEntryDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var scopeTenantId = tenant.Role == Roles.PlatformAdmin ? (Guid?)null : tenant.TenantId;

        var entries = await auditRepo.QueryAsync(
            scopeTenantId, request.EntityType, request.EntityId, request.Action,
            request.From, request.To, request.PageSize, request.Cursor, ct);

        return entries.Select(e => new AuditLogEntryDto(
            e.AuditId, e.TenantId, e.EntityType, e.EntityId, e.Action,
            e.OldValue, e.NewValue, e.PerformedByUserId, e.PerformedByIpAddress,
            e.OccurredAt, e.ChainHash)).ToList();
    }
}
