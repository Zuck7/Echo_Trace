using MediatR;

namespace EchoTrace.Application.Documents.Queries.GetDocumentDownload;

public record GetDocumentDownloadQuery(Guid DocumentId) : IRequest<DocumentDownloadResult>;

public record DocumentDownloadResult(string DownloadUrl, DateTime ExpiresAt, string ContentHash, bool IntegrityVerified);
