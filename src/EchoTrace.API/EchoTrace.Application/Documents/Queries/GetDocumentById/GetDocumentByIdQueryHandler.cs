using EchoTrace.Application.Documents.Queries.GetDocuments;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Documents.Queries.GetDocumentById;

public sealed class GetDocumentByIdQueryHandler(IDocumentRepository documentRepo)
    : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var d = await documentRepo.GetByIdAsync(request.DocumentId, ct);
        if (d is null) return null;

        return new DocumentDto(
            d.DocumentId, d.OrgId, d.DocumentType, d.OriginalFileName, d.BlobPath, d.ContentHash,
            d.FileSizeBytes, d.MimeType, d.IssuedAt, d.ExpiresAt, d.Status, d.UploadedAt);
    }
}
