using EchoTrace.Application.Organizations.Commands.CreateOrganization;
using EchoTrace.Application.Organizations.Queries.GetOrganizationById;
using EchoTrace.Application.Organizations.Queries.GetOrganizations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EchoTrace.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrganizationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Create a new organization within the caller's tenant. Platform Admins onboard buying
    /// orgs; Org Admins use this to add a supplier node to their own supply chain graph
    /// (until invitation-based supplier self-registration — Milestone 1.2 — exists).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "PLATFORM_ADMIN,ORG_ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { orgId = result.OrgId }, result);
    }

    /// <summary>Get organization by ID.</summary>
    [HttpGet("{orgId:guid}")]
    public async Task<IActionResult> GetById(Guid orgId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrganizationByIdQuery(orgId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>List organizations for the current tenant.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrganizationsQuery(), ct);
        return Ok(result);
    }
}
