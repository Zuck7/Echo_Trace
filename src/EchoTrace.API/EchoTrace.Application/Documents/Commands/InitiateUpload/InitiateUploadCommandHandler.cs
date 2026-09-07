using EchoTrace.Application.Common.Interfaces;
using MediatR;

namespace EchoTrace.Application.Documents.Commands.InitiateUpload;

/// <summary>
/// Step 1 of the two-step upload flow (see docs/ADR/003-file-microservice.md): records the
/// caller's intent so <c>confirm-upload</c> can validate the eventual file matches what was
/// declared. The file bytes themselves are never seen by this step — they go straight to
/// <c>confirm-upload</c>, which streams them through the Core API to the File Service, so the
/// File Service's internal auth key never has to leave the server.
/// </summary>
public sealed class InitiateUploadCommandHandler(
    IUploadRequestStore uploadStore,
    ICurrentTenantService tenant) : IRequestHandler<InitiateUploadCommand, InitiateUploadResult>
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public Task<InitiateUploadResult> Handle(InitiateUploadCommand request, CancellationToken ct)
    {
        var uploadRequestId = Guid.NewGuid();

        uploadStore.Save(
            new PendingUpload(
                uploadRequestId,
                tenant.TenantId,
                tenant.OrgId,
                tenant.UserId,
                request.DocumentType,
                request.OriginalFileName,
                request.IssuedAt,
                request.ExpiresAt),
            Ttl);

        return Task.FromResult(new InitiateUploadResult(uploadRequestId, DateTime.UtcNow.Add(Ttl)));
    }
}
