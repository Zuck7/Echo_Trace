using EchoTrace.Domain.Entities;

namespace EchoTrace.Domain.Interfaces.Repositories;

public record SupplyChainNode(
    Guid OrgId,
    string LegalName,
    string Country,
    string Status,
    int Depth,
    string Path);

public record SupplyChainTreeResult(
    Guid RootOrgId,
    List<SupplyChainNode> Nodes,
    List<SupplyChainEdge> Edges);

public interface ISupplyChainRepository
{
    Task<List<SupplyChainEdge>> GetActiveEdgesByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<SupplyChainEdge?> GetEdgeByIdAsync(Guid edgeId, CancellationToken ct = default);
    Task AddEdgeAsync(SupplyChainEdge edge, CancellationToken ct = default);
    Task UpdateEdgeAsync(SupplyChainEdge edge, CancellationToken ct = default);

    /// <summary>Executes the recursive CTE to get the downstream supplier tree.</summary>
    Task<SupplyChainTreeResult> GetSupplierTreeAsync(Guid rootOrgId, Guid tenantId, int maxDepth = -1, CancellationToken ct = default);

    /// <summary>Full trace-back including cert status per node.</summary>
    Task<SupplyChainTreeResult> TraceBackAsync(Guid orgId, Guid tenantId, int maxDepth = -1, CancellationToken ct = default);
}
