namespace EchoTrace.Domain.Entities;

public class User
{
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrgId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(Guid tenantId, Guid orgId, string email, string passwordHash, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePasswordHash(string newHash) => PasswordHash = newHash;
    public void Deactivate() => IsActive = false;
}

public static class Roles
{
    public const string PlatformAdmin = "PLATFORM_ADMIN";
    public const string OrgAdmin = "ORG_ADMIN";
    public const string ComplianceOfficer = "COMPLIANCE_OFFICER";
    public const string SupplierRep = "SUPPLIER_REP";
    public const string Auditor = "AUDITOR";
}
