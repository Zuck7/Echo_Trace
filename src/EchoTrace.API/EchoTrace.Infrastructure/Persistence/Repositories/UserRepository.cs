using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EchoTrace.Infrastructure.Persistence.Repositories;

public class UserRepository(EchoTraceDbContext db) : IUserRepository
{
    // IgnoreQueryFilters: email/id lookups happen before a tenant is known (e.g. during login,
    // where the caller has no JWT yet), so the tenant-scoped global query filter must be bypassed.
    // Email is globally unique (see UX_Users_Email) so this doesn't leak cross-tenant data.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<bool> AnyExistForTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        db.Users.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenantId, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
