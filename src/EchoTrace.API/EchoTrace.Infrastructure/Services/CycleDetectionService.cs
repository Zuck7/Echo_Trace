using EchoTrace.Domain.Interfaces.Repositories;
using EchoTrace.Domain.Interfaces.Services;

namespace EchoTrace.Infrastructure.Services;

public class CycleDetectionService(ISupplyChainRepository supplyChainRepo) : ICycleDetectionService
{
    public async Task<CycleDetectionResult> WouldCreateCycleAsync(
        Guid parentOrgId,
        Guid childOrgId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var edges = await supplyChainRepo.GetActiveEdgesByTenantAsync(tenantId, ct);

        // Build adjacency list from current edges
        var graph = new Dictionary<Guid, List<Guid>>();
        foreach (var edge in edges)
        {
            if (!graph.ContainsKey(edge.ParentOrgId))
                graph[edge.ParentOrgId] = [];
            graph[edge.ParentOrgId].Add(edge.ChildOrgId);
        }

        // Add the proposed edge temporarily
        if (!graph.ContainsKey(parentOrgId))
            graph[parentOrgId] = [];
        graph[parentOrgId].Add(childOrgId);

        // DFS from childOrgId; if we reach parentOrgId, a cycle exists
        var visited = new HashSet<Guid>();
        var path = new List<Guid> { childOrgId };

        return DfsDetectCycle(graph, childOrgId, parentOrgId, visited, path);
    }

    private static CycleDetectionResult DfsDetectCycle(
        Dictionary<Guid, List<Guid>> graph,
        Guid current,
        Guid target,
        HashSet<Guid> visited,
        List<Guid> path)
    {
        if (current == target)
        {
            var cyclePath = string.Join(" → ", path.Select(id => id.ToString()[..8]));
            return new CycleDetectionResult(true, cyclePath);
        }

        visited.Add(current);

        if (!graph.TryGetValue(current, out var neighbors))
            return new CycleDetectionResult(false, null);

        foreach (var neighbor in neighbors)
        {
            if (visited.Contains(neighbor)) continue;
            path.Add(neighbor);
            var result = DfsDetectCycle(graph, neighbor, target, visited, path);
            if (result.WouldCreateCycle) return result;
            path.RemoveAt(path.Count - 1);
        }

        return new CycleDetectionResult(false, null);
    }
}
