using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Application.SupplyChain.Commands.AddEdge;
using EchoTrace.Application.SupplyChain.Commands.RemoveEdge;
using EchoTrace.Application.SupplyChain.Queries.GetSupplyChainTree;
using EchoTrace.Domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EchoTrace.API.Controllers;

[ApiController]
[Route("api/v1/supply-chain")]
[Authorize]
public class SupplyChainController(
    IMediator mediator,
    ICycleDetectionService cycleDetector,
    ICurrentTenantService tenant) : ControllerBase
{
    /// <summary>Add a supplier relationship. Runs cycle detection before persisting.</summary>
    [HttpPost("edges")]
    [Authorize(Roles = "ORG_ADMIN")]
    public async Task<IActionResult> AddEdge([FromBody] AddEdgeCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Created($"/api/v1/supply-chain/edges/{result.EdgeId}", result);
    }

    /// <summary>Deactivate a supplier relationship.</summary>
    [HttpDelete("edges/{edgeId:guid}")]
    [Authorize(Roles = "ORG_ADMIN")]
    public async Task<IActionResult> RemoveEdge(Guid edgeId, CancellationToken ct)
    {
        await mediator.Send(new RemoveEdgeCommand(edgeId), ct);
        return NoContent();
    }

    /// <summary>Get downstream supplier tree from an org.</summary>
    [HttpGet("tree/{orgId:guid}")]
    public async Task<IActionResult> GetTree(Guid orgId, [FromQuery] int depth = -1, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetSupplyChainTreeQuery(orgId, depth), ct);
        return Ok(result);
    }

    /// <summary>Full trace-back report for an org.</summary>
    [HttpGet("trace/{orgId:guid}")]
    public IActionResult TraceBack(Guid orgId, [FromQuery] int depth = -1)
    {
        // TODO: wire up TraceBackQuery in Milestone 2.1
        return Ok(new { message = "TraceBack — implement in Milestone 2.1", orgId });
    }

    /// <summary>Pre-flight cycle check without persisting the edge.</summary>
    [HttpPost("cycle-check")]
    public async Task<IActionResult> CycleCheck([FromBody] CycleCheckRequest request, CancellationToken ct)
    {
        var result = await cycleDetector.WouldCreateCycleAsync(
            request.ParentOrgId, request.ChildOrgId, tenant.TenantId, ct);
        return Ok(result);
    }
}

public record CycleCheckRequest(Guid ParentOrgId, Guid ChildOrgId);
