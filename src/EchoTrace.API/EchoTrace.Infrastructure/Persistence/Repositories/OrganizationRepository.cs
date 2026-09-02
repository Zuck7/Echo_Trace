using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EchoTrace.Infrastructure.Persistence.Repositories;

public class OrganizationRepository(EchoTraceDbContext db) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid orgId, CancellationToken ct = default) =>
        db.Organizations.FirstOrDefaultAsync(o => o.OrgId == orgId, ct);

    public Task<List<Organization>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        db.Organizations.Where(o => o.TenantId == tenantId).OrderBy(o => o.LegalName).ToListAsync(ct);

    public async Task AddAsync(Organization org, CancellationToken ct = default)
    {
        db.Organizations.Add(org);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Organization org, CancellationToken ct = default)
    {
        db.Organizations.Update(org);
        await db.SaveChangesAsync(ct);
    }
}
