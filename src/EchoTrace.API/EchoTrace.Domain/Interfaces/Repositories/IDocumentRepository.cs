using EchoTrace.Domain.Entities;

namespace EchoTrace.Domain.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<List<Document>> GetByOrgIdAsync(Guid orgId, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);
    Task<List<Document>> GetExpiringBeforeAsync(DateOnly cutoff, CancellationToken ct = default);

    Task<List<Document>> QueryAsync(
        Guid orgId, string? documentType, string? status, int pageSize, string? cursor, CancellationToken ct = default);
}
