using EchoTrace.Application.SupplyChain.Commands.AddEdge;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EchoTrace.API.Controllers;

[ApiController]
[Route("api/v1/supply-chain")]
[Authorize]
public class SupplyChainController(IMediator mediator) : ControllerBase
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
    public IActionResult RemoveEdge(Guid edgeId)
    {
        // TODO: wire up RemoveEdgeCommand in Milestone 1.4
        return NoContent();
    }

    /// <summary>Get downstream supplier tree from an org.</summary>
    [HttpGet("tree/{orgId:guid}")]
    public IActionResult GetTree(Guid orgId, [FromQuery] int depth = -1)
    {
        // TODO: wire up GetSupplyChainTreeQuery in Milestone 1.4
        return Ok(new { message = "GetTree — implement in Milestone 1.4", orgId, depth });
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
    public IActionResult CycleCheck([FromBody] CycleCheckRequest request)
    {
        // TODO: wire up cycle detection service directly in Milestone 1.4
        return Ok(new { message = "CycleCheck — implement in Milestone 1.4" });
    }
}

public record CycleCheckRequest(Guid ParentOrgId, Guid ChildOrgId);
