using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Documents.Commands.ConfirmUpload;

public sealed class ConfirmUploadCommandHandler(
    IUploadRequestStore uploadStore,
    IFileServiceClient fileService,
    IDocumentRepository documentRepo,
    ICurrentTenantService tenant,
    IAuditService audit) : IRequestHandler<ConfirmUploadCommand, ConfirmUploadResult>
{
    public async Task<ConfirmUploadResult> Handle(ConfirmUploadCommand request, CancellationToken ct)
    {
        var pending = uploadStore.TryTake(request.UploadRequestId)
            ?? throw new KeyNotFoundException($"Upload request {request.UploadRequestId} not found or expired.");

        if (pending.TenantId != tenant.TenantId)
            throw new UnauthorizedAccessException("Upload request does not belong to the caller's tenant.");

        var uploadResult = await fileService.UploadAsync(request.FileStream, request.FileName, request.MimeType, ct);

        var document = Document.Create(
            pending.TenantId,
            pending.OrgId,
            pending.DocumentType,
            pending.OriginalFileName,
            uploadResult.BlobPath,
            uploadResult.ContentHash,
            uploadResult.FileSizeBytes,
            uploadResult.MimeType,
            pending.IssuedAt,
            pending.ExpiresAt,
            pending.UserId);

        await documentRepo.AddAsync(document, ct);

        await audit.LogAsync("Document", document.DocumentId.ToString(), "CREATE", null, document, ct);

        return new ConfirmUploadResult(document.DocumentId, document.ContentHash, document.Status, document.UploadedAt);
    }
}
