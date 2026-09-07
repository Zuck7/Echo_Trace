using MediatR;

namespace EchoTrace.Application.Documents.Queries.GetDocuments;

public record GetDocumentsQuery(
    Guid OrgId,
    string? DocumentType,
    string? Status,
    int PageSize = 50,
    string? Cursor = null) : IRequest<List<DocumentDto>>;

public record DocumentDto(
    Guid DocumentId,
    Guid OrgId,
    string DocumentType,
    string OriginalFileName,
    string BlobPath,
    string ContentHash,
    long FileSizeBytes,
    string MimeType,
    DateOnly? IssuedAt,
    DateOnly? ExpiresAt,
    string Status,
    DateTime UploadedAt);
