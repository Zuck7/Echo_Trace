using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Application.Documents.Commands.ConfirmUpload;
using EchoTrace.Application.Documents.Commands.InitiateUpload;
using EchoTrace.Application.Documents.Commands.RevokeDocument;
using EchoTrace.Application.Documents.Queries.GetDocumentById;
using EchoTrace.Application.Documents.Queries.GetDocumentDownload;
using EchoTrace.Application.Documents.Queries.GetDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EchoTrace.API.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController(IMediator mediator, IFileServiceClient fileService) : ControllerBase
{
    /// <summary>List certification documents for an organization.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid orgId, [FromQuery] string? documentType, [FromQuery] string? status,
        [FromQuery] int pageSize = 50, [FromQuery] string? cursor = null, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetDocumentsQuery(orgId, documentType, status, pageSize, cursor), ct);
        return Ok(result);
    }

    /// <summary>Get document metadata.</summary>
    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetById(Guid documentId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDocumentByIdQuery(documentId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Step 1 of the upload flow: declare what's about to be uploaded.</summary>
    [HttpPost("upload-request")]
    [Authorize(Roles = "ORG_ADMIN,COMPLIANCE_OFFICER")]
    public async Task<IActionResult> InitiateUpload([FromBody] InitiateUploadCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Step 2: send the actual file. Accepts multipart/form-data with a "file" part and the
    /// "uploadRequestId" from step 1; the Core API streams the bytes to the File Service itself
    /// (see docs/ADR/003-file-microservice.md), so the browser never talks to the File Service or
    /// sees its internal auth key.
    /// </summary>
    [HttpPost("confirm-upload")]
    [Authorize(Roles = "ORG_ADMIN,COMPLIANCE_OFFICER")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ConfirmUpload(
        [FromForm] Guid uploadRequestId, [FromForm] IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var command = new ConfirmUploadCommand(uploadRequestId, stream, file.FileName, file.ContentType);
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { documentId = result.DocumentId }, result);
    }

    /// <summary>Get a time-limited download link. Re-verifies the stored hash before issuing it.</summary>
    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> GetDownloadLink(Guid documentId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDocumentDownloadQuery(documentId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Streams the file itself. Used by the link returned from <c>/download</c>, which is the
    /// step that re-verifies the hash — this endpoint just serves bytes.
    /// </summary>
    [HttpGet("{documentId:guid}/file")]
    public async Task<IActionResult> GetFile(Guid documentId, CancellationToken ct)
    {
        var document = await mediator.Send(new GetDocumentByIdQuery(documentId), ct);
        if (document is null) return NotFound();

        var stream = await fileService.DownloadAsync(document.BlobPath, ct);
        return File(stream, document.MimeType, document.OriginalFileName);
    }

    /// <summary>Revoke (soft-delete) a document.</summary>
    [HttpDelete("{documentId:guid}")]
    [Authorize(Roles = "ORG_ADMIN")]
    public async Task<IActionResult> Revoke(Guid documentId, CancellationToken ct)
    {
        await mediator.Send(new RevokeDocumentCommand(documentId), ct);
        return NoContent();
    }
}
