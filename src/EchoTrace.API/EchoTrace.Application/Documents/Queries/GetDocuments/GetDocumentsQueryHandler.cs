using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Documents.Queries.GetDocuments;

public sealed class GetDocumentsQueryHandler(IDocumentRepository documentRepo)
    : IRequestHandler<GetDocumentsQuery, List<DocumentDto>>
{
    public async Task<List<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var documents = await documentRepo.QueryAsync(
            request.OrgId, request.DocumentType, request.Status, request.PageSize, request.Cursor, ct);

        return documents.Select(d => new DocumentDto(
            d.DocumentId, d.OrgId, d.DocumentType, d.OriginalFileName, d.BlobPath, d.ContentHash,
            d.FileSizeBytes, d.MimeType, d.IssuedAt, d.ExpiresAt, d.Status, d.UploadedAt)).ToList();
    }
}
