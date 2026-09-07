using MediatR;

namespace EchoTrace.Application.Documents.Commands.InitiateUpload;

public record InitiateUploadCommand(
    string DocumentType,
    string OriginalFileName,
    DateOnly? IssuedAt,
    DateOnly? ExpiresAt) : IRequest<InitiateUploadResult>;

public record InitiateUploadResult(Guid UploadRequestId, DateTime ExpiresAt);
