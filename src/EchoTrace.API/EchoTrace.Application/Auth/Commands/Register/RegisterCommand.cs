using MediatR;

namespace EchoTrace.Application.Auth.Commands.Register;

/// <summary>
/// Registers a brand-new tenant, organization, and its first ORG_ADMIN user.
/// Invitation-token-based joining of an *existing* tenant (per API_DESIGN.md) is not yet
/// implemented — there is no Invitation entity/table. This covers the self-onboarding path only.
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string OrgLegalName,
    string OrgCountry,
    string? OrgContactEmail) : IRequest<RegisterResult>;

public record RegisterResult(Guid UserId, Guid OrgId, Guid TenantId, string Email, string Role);
