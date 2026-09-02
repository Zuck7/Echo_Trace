using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.SupplyChain.Commands.RemoveEdge;

public sealed class RemoveEdgeCommandHandler(
    ISupplyChainRepository supplyChainRepo,
    IAuditService audit) : IRequestHandler<RemoveEdgeCommand>
{
    public async Task Handle(RemoveEdgeCommand request, CancellationToken ct)
    {
        var edge = await supplyChainRepo.GetEdgeByIdAsync(request.EdgeId, ct)
            ?? throw new KeyNotFoundException($"Supply chain edge {request.EdgeId} not found.");

        edge.Deactivate();
        await supplyChainRepo.UpdateEdgeAsync(edge, ct);

        await audit.LogAsync("SupplyChainEdge", edge.EdgeId.ToString(), "DELETE", null,
            new { edge.EdgeId, edge.IsActive }, ct);
    }
}
