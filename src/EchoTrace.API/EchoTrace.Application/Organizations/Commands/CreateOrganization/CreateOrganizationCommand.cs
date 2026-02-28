using MediatR;

namespace EchoTrace.Application.Organizations.Commands.CreateOrganization;

public record CreateOrganizationCommand(
    string LegalName,
    string Country,
    string ContactEmail,
    string? IndustrySector,
    string? RegistrationNumber) : IRequest<CreateOrganizationResult>;

public record CreateOrganizationResult(Guid OrgId, Guid TenantId, string LegalName, string Status);
