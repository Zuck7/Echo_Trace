namespace EchoTrace.Domain.Interfaces.Services;

public record CycleDetectionResult(bool WouldCreateCycle, string? CyclePath);

public interface ICycleDetectionService
{
    /// <summary>
    /// Checks if adding an edge from parentOrgId → childOrgId would create a cycle.
    /// Runs a DFS on the current graph for the tenant.
    /// </summary>
    Task<CycleDetectionResult> WouldCreateCycleAsync(
        Guid parentOrgId,
        Guid childOrgId,
        Guid tenantId,
        CancellationToken ct = default);
}
