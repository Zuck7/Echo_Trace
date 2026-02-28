using EchoTrace.Domain.Entities;

namespace EchoTrace.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid orgId, CancellationToken ct = default);
    Task<List<Organization>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Organization org, CancellationToken ct = default);
    Task UpdateAsync(Organization org, CancellationToken ct = default);
}
