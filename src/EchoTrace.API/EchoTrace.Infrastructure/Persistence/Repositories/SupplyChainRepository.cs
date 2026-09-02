using System.Data;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EchoTrace.Infrastructure.Persistence.Repositories;

public class SupplyChainRepository(EchoTraceDbContext db) : ISupplyChainRepository
{
    public Task<List<SupplyChainEdge>> GetActiveEdgesByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        db.SupplyChainEdges.Where(e => e.TenantId == tenantId && e.IsActive).ToListAsync(ct);

    public Task<SupplyChainEdge?> GetEdgeByIdAsync(Guid edgeId, CancellationToken ct = default) =>
        db.SupplyChainEdges.FirstOrDefaultAsync(e => e.EdgeId == edgeId, ct);

    public async Task AddEdgeAsync(SupplyChainEdge edge, CancellationToken ct = default)
    {
        db.SupplyChainEdges.Add(edge);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateEdgeAsync(SupplyChainEdge edge, CancellationToken ct = default)
    {
        db.SupplyChainEdges.Update(edge);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Executes the recursive CTE from docs/DATA_MODEL.md §5.1 to walk the downstream
    /// supplier tree, plus a second pass to pull in the active edges among the returned nodes.
    /// </summary>
    public async Task<SupplyChainTreeResult> GetSupplierTreeAsync(
        Guid rootOrgId, Guid tenantId, int maxDepth = -1, CancellationToken ct = default)
    {
        var effectiveMaxDepth = maxDepth < 0 ? 100 : maxDepth;
        var nodes = new List<SupplyChainNode>();

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                WITH SupplierTree AS (
                    SELECT o.OrgId, o.LegalName, o.Country, o.Status,
                           0 AS Depth, CAST(o.OrgId AS NVARCHAR(MAX)) AS Path
                    FROM Organizations o
                    WHERE o.OrgId = @RootOrgId AND o.TenantId = @TenantId

                    UNION ALL

                    SELECT child.OrgId, child.LegalName, child.Country, child.Status,
                           st.Depth + 1, st.Path + N' -> ' + CAST(child.OrgId AS NVARCHAR(MAX))
                    FROM SupplyChainEdges e
                    INNER JOIN Organizations child ON child.OrgId = e.ChildOrgId
                    INNER JOIN SupplierTree st ON st.OrgId = e.ParentOrgId
                    WHERE e.IsActive = 1 AND e.TenantId = @TenantId AND st.Depth < @MaxDepth
                )
                SELECT DISTINCT OrgId, LegalName, Country, Status, Depth, Path
                FROM SupplierTree
                ORDER BY Depth, LegalName;
                """;

            AddParam(command, "@RootOrgId", rootOrgId);
            AddParam(command, "@TenantId", tenantId);
            AddParam(command, "@MaxDepth", effectiveMaxDepth);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                nodes.Add(new SupplyChainNode(
                    reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetInt32(4), reader.GetString(5)));
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }

        var nodeIds = nodes.Select(n => n.OrgId).ToHashSet();
        var edges = await db.SupplyChainEdges
            .Where(e => e.TenantId == tenantId && e.IsActive
                && nodeIds.Contains(e.ParentOrgId) && nodeIds.Contains(e.ChildOrgId))
            .ToListAsync(ct);

        return new SupplyChainTreeResult(rootOrgId, nodes, edges);
    }

    public Task<SupplyChainTreeResult> TraceBackAsync(
        Guid orgId, Guid tenantId, int maxDepth = -1, CancellationToken ct = default) =>
        // Phase 2 (Milestone 2.1) will enrich each node with certification/compliance status.
        GetSupplierTreeAsync(orgId, tenantId, maxDepth, ct);

    private static void AddParam(IDbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}
