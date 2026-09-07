using System.Security.Cryptography;
using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Exceptions;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Documents.Queries.GetDocumentDownload;

/// <summary>
/// Re-verifies the stored SHA-256 hash against the blob's current bytes before handing back a
/// download link — see docs/API_DESIGN.md §6 and DocumentIntegrityViolationException. The actual
/// bytes are served separately by <c>GET /documents/{id}/file</c> once this check passes.
/// </summary>
public sealed class GetDocumentDownloadQueryHandler(
    IDocumentRepository documentRepo,
    IFileServiceClient fileService) : IRequestHandler<GetDocumentDownloadQuery, DocumentDownloadResult>
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);

    public async Task<DocumentDownloadResult> Handle(GetDocumentDownloadQuery request, CancellationToken ct)
    {
        var document = await documentRepo.GetByIdAsync(request.DocumentId, ct)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        await using var blob = await fileService.DownloadAsync(document.BlobPath, ct);
        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(blob, ct)).ToLowerInvariant();

        if (!string.Equals(actualHash, document.ContentHash, StringComparison.OrdinalIgnoreCase))
            throw new DocumentIntegrityViolationException(document.DocumentId);

        return new DocumentDownloadResult(
            $"/api/v1/documents/{document.DocumentId}/file",
            DateTime.UtcNow.Add(LinkLifetime),
            document.ContentHash,
            IntegrityVerified: true);
    }
}
