using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandHandler(
    IOrganizationRepository orgRepo,
    ICurrentTenantService tenant,
    IAuditService audit) : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResult>
{
    public async Task<CreateOrganizationResult> Handle(CreateOrganizationCommand request, CancellationToken ct)
    {
        var org = Organization.Create(
            tenant.TenantId,
            request.LegalName,
            request.Country,
            request.ContactEmail,
            request.IndustrySector,
            request.RegistrationNumber);

        await orgRepo.AddAsync(org, ct);

        await audit.LogAsync("Organization", org.OrgId.ToString(), "CREATE", null, org, ct);

        return new CreateOrganizationResult(org.OrgId, org.TenantId, org.LegalName, org.Status);
    }
}
