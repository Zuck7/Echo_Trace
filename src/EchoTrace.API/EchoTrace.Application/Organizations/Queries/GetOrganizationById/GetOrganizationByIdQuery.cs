using MediatR;

namespace EchoTrace.Application.Organizations.Queries.GetOrganizationById;

public record GetOrganizationByIdQuery(Guid OrgId) : IRequest<OrganizationDto?>;

public record OrganizationDto(
    Guid OrgId,
    Guid TenantId,
    string LegalName,
    string Country,
    string? IndustrySector,
    string? RegistrationNumber,
    string ContactEmail,
    string? Address,
    int? EsgRiskScore,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
