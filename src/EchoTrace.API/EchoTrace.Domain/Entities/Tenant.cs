namespace EchoTrace.Domain.Entities;

public class Tenant
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PlanTier { get; private set; } = "STANDARD";
    public string Status { get; private set; } = "Active";
    public DateTime CreatedAt { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string planTier = "STANDARD")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tenant
        {
            TenantId = Guid.NewGuid(),
            Name = name,
            PlanTier = planTier,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Suspend() => Status = "Suspended";
    public void Activate() => Status = "Active";
}
