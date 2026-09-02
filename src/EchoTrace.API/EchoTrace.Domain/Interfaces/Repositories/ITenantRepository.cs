using EchoTrace.Domain.Entities;

namespace EchoTrace.Domain.Interfaces.Repositories;

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
}
