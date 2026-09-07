namespace EchoTrace.Application.Common.Interfaces;

public record PendingUpload(
    Guid UploadRequestId,
    Guid TenantId,
    Guid OrgId,
    Guid UserId,
    string DocumentType,
    string OriginalFileName,
    DateOnly? IssuedAt,
    DateOnly? ExpiresAt);

/// <summary>
/// Holds the metadata submitted in <c>POST /documents/upload-request</c> until the matching
/// <c>POST /documents/confirm-upload</c> arrives with the file bytes. In-memory and single-use by
/// design — Milestone 1.5 targets a single-instance Phase 1 deployment (see docs/ROADMAP.md); a
/// multi-instance deployment would need this backed by Redis (Phase 3) instead.
/// </summary>
public interface IUploadRequestStore
{
    void Save(PendingUpload upload, TimeSpan ttl);

    PendingUpload? TryTake(Guid uploadRequestId);
}
