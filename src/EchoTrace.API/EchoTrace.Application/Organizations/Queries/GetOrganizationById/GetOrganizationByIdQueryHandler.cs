using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Organizations.Queries.GetOrganizationById;

public sealed class GetOrganizationByIdQueryHandler(IOrganizationRepository orgRepo)
    : IRequestHandler<GetOrganizationByIdQuery, OrganizationDto?>
{
    public async Task<OrganizationDto?> Handle(GetOrganizationByIdQuery request, CancellationToken ct)
    {
        var org = await orgRepo.GetByIdAsync(request.OrgId, ct);
        if (org is null) return null;

        return new OrganizationDto(
            org.OrgId, org.TenantId, org.LegalName, org.Country, org.IndustrySector,
            org.RegistrationNumber, org.ContactEmail, org.Address, org.EsgRiskScore,
            org.Status, org.CreatedAt, org.UpdatedAt);
    }
}
