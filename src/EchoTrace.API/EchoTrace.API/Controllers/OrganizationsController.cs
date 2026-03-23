using EchoTrace.Application.Organizations.Commands.CreateOrganization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EchoTrace.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrganizationsController(IMediator mediator) : ControllerBase
{
    /// <summary>Create a new organization (Platform Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "PLATFORM_ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { orgId = result.OrgId }, result);
    }

    /// <summary>Get organization by ID.</summary>
    [HttpGet("{orgId:guid}")]
    public IActionResult GetById(Guid orgId)
    {
        // TODO: wire up GetOrganizationByIdQuery in Milestone 1.3
        return Ok(new { message = "GetById — implement in Milestone 1.3", orgId });
    }

    /// <summary>List organizations for the current tenant.</summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        // TODO: wire up GetOrganizationsQuery in Milestone 1.3
        return Ok(new { message = "GetAll — implement in Milestone 1.3" });
    }
}
