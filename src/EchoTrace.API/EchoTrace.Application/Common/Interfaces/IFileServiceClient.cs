namespace EchoTrace.Application.Common.Interfaces;

public record FileUploadResult(string BlobPath, string ContentHash, long FileSizeBytes, string MimeType);

public interface IFileServiceClient
{
    Task<FileUploadResult> UploadAsync(Stream fileStream, string fileName, string mimeType, CancellationToken ct = default);

    Task<Stream> DownloadAsync(string blobPath, CancellationToken ct = default);
}
