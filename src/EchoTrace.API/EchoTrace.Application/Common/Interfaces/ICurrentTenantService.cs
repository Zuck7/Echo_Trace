namespace EchoTrace.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    Guid TenantId { get; }
    Guid UserId { get; }
    Guid OrgId { get; }
    string Role { get; }
    string? IpAddress { get; }
}
