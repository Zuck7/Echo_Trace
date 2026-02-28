using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Exceptions;
using EchoTrace.Domain.Interfaces.Repositories;
using EchoTrace.Domain.Interfaces.Services;
using MediatR;

namespace EchoTrace.Application.SupplyChain.Commands.AddEdge;

public sealed class AddEdgeCommandHandler(
    ISupplyChainRepository supplyChainRepo,
    ICycleDetectionService cycleDetector,
    ICurrentTenantService tenant,
    IAuditService audit) : IRequestHandler<AddEdgeCommand, AddEdgeResult>
{
    public async Task<AddEdgeResult> Handle(AddEdgeCommand request, CancellationToken ct)
    {
        var cycleResult = await cycleDetector.WouldCreateCycleAsync(
            tenant.OrgId, request.ChildOrgId, tenant.TenantId, ct);

        if (cycleResult.WouldCreateCycle)
            throw new CycleDetectedException(cycleResult.CyclePath!);

        var edge = SupplyChainEdge.Create(
            tenant.TenantId,
            tenant.OrgId,
            request.ChildOrgId,
            request.RelationshipType,
            request.MaterialId,
            request.ValidFrom);

        await supplyChainRepo.AddEdgeAsync(edge, ct);

        await audit.LogAsync(
            entityType: "SupplyChainEdge",
            entityId: edge.EdgeId.ToString(),
            action: "CREATE",
            oldValue: null,
            newValue: edge,
            ct: ct);

        return new AddEdgeResult(
            edge.EdgeId,
            edge.ParentOrgId,
            edge.ChildOrgId,
            edge.RelationshipType,
            edge.IsActive,
            edge.CreatedAt);
    }
}
