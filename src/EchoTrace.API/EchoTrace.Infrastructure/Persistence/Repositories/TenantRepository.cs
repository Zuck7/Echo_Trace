using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;

namespace EchoTrace.Infrastructure.Persistence.Repositories;

public class TenantRepository(EchoTraceDbContext db) : ITenantRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
    }
}
