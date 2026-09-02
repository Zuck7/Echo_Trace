using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Application.Organizations.Queries.GetOrganizationById;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsQueryHandler(
    IOrganizationRepository orgRepo,
    ICurrentTenantService tenant) : IRequestHandler<GetOrganizationsQuery, List<OrganizationDto>>
{
    public async Task<List<OrganizationDto>> Handle(GetOrganizationsQuery request, CancellationToken ct)
    {
        var orgs = await orgRepo.GetByTenantAsync(tenant.TenantId, ct);

        return orgs.Select(org => new OrganizationDto(
            org.OrgId, org.TenantId, org.LegalName, org.Country, org.IndustrySector,
            org.RegistrationNumber, org.ContactEmail, org.Address, org.EsgRiskScore,
            org.Status, org.CreatedAt, org.UpdatedAt)).ToList();
    }
}
