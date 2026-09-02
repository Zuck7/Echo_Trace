using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    ITenantRepository tenantRepo,
    IOrganizationRepository orgRepo,
    IUserRepository userRepo,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        var existing = await userRepo.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var tenant = Tenant.Create(request.OrgLegalName);
        await tenantRepo.AddAsync(tenant, ct);

        var org = Organization.Create(
            tenant.TenantId,
            request.OrgLegalName,
            request.OrgCountry,
            request.OrgContactEmail ?? request.Email);
        org.Activate();
        await orgRepo.AddAsync(org, ct);

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(tenant.TenantId, org.OrgId, request.Email, passwordHash, Roles.OrgAdmin);
        await userRepo.AddAsync(user, ct);

        return new RegisterResult(user.UserId, org.OrgId, tenant.TenantId, user.Email, user.Role);
    }
}
