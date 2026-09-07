using System.Collections.Concurrent;
using System.Security.Cryptography;
using EchoTrace.Application.Common.Interfaces;

namespace EchoTrace.Tests.Integration;

/// <summary>
/// Stateful in-memory stand-in for the Node.js file service, used by integration tests so the
/// full HTTP flow (upload-request → confirm-upload → download) can run without a live MinIO/file
/// service. Computes a real SHA-256 hash on upload and replays the same bytes on download, so
/// integrity re-verification behaves exactly as it would against the real service.
/// </summary>
public class FakeFileServiceClient : IFileServiceClient
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

    public async Task<FileUploadResult> UploadAsync(
        Stream fileStream, string fileName, string mimeType, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        var blobPath = $"docs/{Guid.NewGuid()}-{fileName}";
        _blobs[blobPath] = bytes;

        var contentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new FileUploadResult(blobPath, contentHash, bytes.LongLength, mimeType);
    }

    public Task<Stream> DownloadAsync(string blobPath, CancellationToken ct = default)
    {
        if (!_blobs.TryGetValue(blobPath, out var bytes))
            throw new InvalidOperationException($"Fake file service has no blob at {blobPath}.");

        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    /// <summary>Simulates tampering with a stored blob after upload, for integrity-check tests.</summary>
    public void Corrupt(string blobPath) => _blobs[blobPath] = "tampered"u8.ToArray();
}
