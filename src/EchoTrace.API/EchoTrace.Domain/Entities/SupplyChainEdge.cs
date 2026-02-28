using EchoTrace.Domain.Exceptions;

namespace EchoTrace.Domain.Entities;

public class SupplyChainEdge
{
    public Guid EdgeId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ParentOrgId { get; private set; }
    public Guid ChildOrgId { get; private set; }
    public Guid? MaterialId { get; private set; }
    public string RelationshipType { get; private set; } = "SOURCES";
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SupplyChainEdge() { }

    public static SupplyChainEdge Create(
        Guid tenantId,
        Guid parentOrgId,
        Guid childOrgId,
        string relationshipType = "SOURCES",
        Guid? materialId = null,
        DateOnly? validFrom = null)
    {
        if (parentOrgId == childOrgId)
            throw new InvalidOperationException("An organization cannot be its own supplier.");

        return new SupplyChainEdge
        {
            EdgeId = Guid.NewGuid(),
            TenantId = tenantId,
            ParentOrgId = parentOrgId,
            ChildOrgId = childOrgId,
            MaterialId = materialId,
            RelationshipType = relationshipType,
            ValidFrom = validFrom ?? DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
}
