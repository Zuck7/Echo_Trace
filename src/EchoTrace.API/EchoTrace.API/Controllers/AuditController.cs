using EchoTrace.Application.Audit.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EchoTrace.API.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize]
public class AuditController(IMediator mediator) : ControllerBase
{
    /// <summary>Tamper-evident audit trail. Each entry's ChainHash covers the previous entry's
    /// hash plus this entry's payload, so altering any stored row breaks every hash after it.</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] string? action,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int pageSize = 50, [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetAuditLogsQuery(entityType, entityId, action, from, to, pageSize, cursor), ct);
        return Ok(result);
    }
}
