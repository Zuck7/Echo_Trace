using System.Security.Claims;
using EchoTrace.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EchoTrace.Infrastructure.Services;

public class CurrentTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenantService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid TenantId => GetGuidClaim("tenantId");
    public Guid UserId => GetGuidClaim(ClaimTypes.NameIdentifier);
    public Guid OrgId => GetGuidClaim("orgId");
    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private Guid GetGuidClaim(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
