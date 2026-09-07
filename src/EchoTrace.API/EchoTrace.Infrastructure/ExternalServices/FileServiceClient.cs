using System.Net.Http.Headers;
using System.Net.Http.Json;
using EchoTrace.Application.Common.Interfaces;

namespace EchoTrace.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for the internal Node.js file service (docs/ADR/003-file-microservice.md). The
/// Core API is the only caller — browsers never see the file service's base URL or its
/// <c>X-Internal-Key</c>, which is attached to every request by the typed-client registration in
/// Program.cs rather than by this class, so it can't accidentally be omitted on a new call site.
/// </summary>
public class FileServiceClient(HttpClient httpClient) : IFileServiceClient
{
    private record UploadResponse(string BlobPath, string ContentHash, long FileSizeBytes, string MimeType);

    public async Task<FileUploadResult> UploadAsync(
        Stream fileStream, string fileName, string mimeType, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
        content.Add(streamContent, "file", fileName);
        content.Headers.Add("X-Upload-Request-Id", Guid.NewGuid().ToString());

        using var response = await httpClient.PostAsync("/upload", content, ct);
        await ThrowIfUnsuccessful(response, ct);

        var body = await response.Content.ReadFromJsonAsync<UploadResponse>(ct)
            ?? throw new InvalidOperationException("File service returned an empty upload response.");

        return new FileUploadResult(body.BlobPath, body.ContentHash, body.FileSizeBytes, body.MimeType);
    }

    public async Task<Stream> DownloadAsync(string blobPath, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/file/{blobPath}", HttpCompletionOption.ResponseHeadersRead, ct);
        await ThrowIfUnsuccessful(response, ct);
        return await response.Content.ReadAsStreamAsync(ct);
    }

    private static async Task ThrowIfUnsuccessful(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"File service call failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }
}
