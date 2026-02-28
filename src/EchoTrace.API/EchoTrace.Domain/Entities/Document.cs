namespace EchoTrace.Domain.Entities;

public class Document
{
    public Guid DocumentId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrgId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string BlobPath { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public DateOnly? IssuedAt { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public string Status { get; private set; } = "Active";
    public Guid? UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Document() { }

    public static Document Create(
        Guid tenantId,
        Guid orgId,
        string documentType,
        string originalFileName,
        string blobPath,
        string contentHash,
        long fileSizeBytes,
        string mimeType,
        DateOnly? issuedAt,
        DateOnly? expiresAt,
        Guid? uploadedByUserId)
    {
        return new Document
        {
            DocumentId = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            DocumentType = documentType,
            OriginalFileName = originalFileName,
            BlobPath = blobPath,
            ContentHash = contentHash,
            FileSizeBytes = fileSizeBytes,
            MimeType = mimeType,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            UploadedByUserId = uploadedByUserId,
            Status = "Active",
            UploadedAt = DateTime.UtcNow
        };
    }

    public void Revoke() => Status = "Revoked";
    public void MarkExpired() => Status = "Expired";
}
