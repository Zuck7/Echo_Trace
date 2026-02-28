namespace EchoTrace.Domain.Entities;

public class Organization
{
    public Guid OrgId { get; private set; }
    public Guid TenantId { get; private set; }
    public string LegalName { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? IndustrySector { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public int? EsgRiskScore { get; private set; }
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Organization() { }

    public static Organization Create(
        Guid tenantId,
        string legalName,
        string country,
        string contactEmail,
        string? industrySector = null,
        string? registrationNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);

        return new Organization
        {
            OrgId = Guid.NewGuid(),
            TenantId = tenantId,
            LegalName = legalName,
            Country = country,
            ContactEmail = contactEmail,
            IndustrySector = industrySector,
            RegistrationNumber = registrationNumber,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        Status = "Active";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = "Suspended";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string? contactEmail, int? esgRiskScore, string? address, string? industrySector)
    {
        if (!string.IsNullOrWhiteSpace(contactEmail)) ContactEmail = contactEmail;
        if (esgRiskScore.HasValue) EsgRiskScore = esgRiskScore;
        if (address is not null) Address = address;
        if (industrySector is not null) IndustrySector = industrySector;
        UpdatedAt = DateTime.UtcNow;
    }
}
