using MediatR;

namespace EchoTrace.Application.Documents.Commands.ConfirmUpload;

public record ConfirmUploadCommand(
    Guid UploadRequestId,
    Stream FileStream,
    string FileName,
    string MimeType) : IRequest<ConfirmUploadResult>;

public record ConfirmUploadResult(Guid DocumentId, string ContentHash, string Status, DateTime UploadedAt);
