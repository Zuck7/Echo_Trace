using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EchoTrace.Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EchoTrace.Tests.Integration;

/// <summary>
/// Exercises Milestone 1.5's Definition of Done end-to-end over real HTTP: an Org Admin can
/// upload a document, the system stores it with its hash, and download re-verifies that hash.
/// The real Node.js file service isn't run here — <see cref="FakeFileServiceClient"/> stands in
/// for it, matching how <see cref="CustomWebApplicationFactory"/> already swaps SQL Server for
/// EF Core InMemory.
/// </summary>
public sealed class DocumentsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    private record RegisterRequest(string Email, string Password, string OrgLegalName, string OrgCountry);
    private record LoginRequest(string Email, string Password);
    private record LoginResponse(string AccessToken, int ExpiresIn, string TokenType);
    private record RegisterResponse(Guid OrgId);
    private record InitiateUploadResponse(Guid UploadRequestId, DateTime ExpiresAt);
    private record ConfirmUploadResponse(Guid DocumentId, string ContentHash, string Status, DateTime UploadedAt);
    private record DocumentDto(Guid DocumentId, string Status);
    private record DownloadLinkResponse(string DownloadUrl, DateTime ExpiresAt, string ContentHash, bool IntegrityVerified);

    public DocumentsFlowTests(CustomWebApplicationFactory factory)
    {
        var customized = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IFileServiceClient>(new FakeFileServiceClient())));

        _client = customized.CreateClient();
    }

    private async Task<(string Token, Guid OrgId)> RegisterAndLogin()
    {
        var email = $"{Guid.NewGuid()}@acme.test";
        var register = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "SecureP@ss123", "Acme Test Corp", "US"));
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "SecureP@ss123"));
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();

        return (token!.AccessToken, registered!.OrgId);
    }

    private static MultipartFormDataContent BuildUploadBody(Guid uploadRequestId, byte[] fileBytes)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(uploadRequestId.ToString()), "uploadRequestId" },
        };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "iso14001-cert.pdf");
        return content;
    }

    [Fact]
    public async Task Org_admin_can_upload_a_document_and_download_re_verifies_its_hash()
    {
        var (token, orgId) = await RegisterAndLogin();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var initiate = await _client.PostAsJsonAsync("/api/v1/documents/upload-request",
            new { documentType = "ISO_14001", originalFileName = "iso14001-cert.pdf", issuedAt = (string?)null, expiresAt = (string?)null });
        initiate.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadRequest = await initiate.Content.ReadFromJsonAsync<InitiateUploadResponse>();

        var fileBytes = Encoding.UTF8.GetBytes("%PDF-1.4 fake certificate content for testing");
        var confirm = await _client.PostAsync("/api/v1/documents/confirm-upload",
            BuildUploadBody(uploadRequest!.UploadRequestId, fileBytes));
        confirm.StatusCode.Should().Be(HttpStatusCode.Created);
        var confirmed = await confirm.Content.ReadFromJsonAsync<ConfirmUploadResponse>();
        confirmed!.Status.Should().Be("Active");

        var list = await _client.GetFromJsonAsync<List<DocumentDto>>($"/api/v1/documents?orgId={orgId}");
        list.Should().ContainSingle(d => d.DocumentId == confirmed.DocumentId);

        var download = await _client.GetAsync($"/api/v1/documents/{confirmed.DocumentId}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadLink = await download.Content.ReadFromJsonAsync<DownloadLinkResponse>();
        downloadLink!.IntegrityVerified.Should().BeTrue();
        downloadLink.ContentHash.Should().Be(confirmed.ContentHash);

        var file = await _client.GetAsync($"/api/v1/documents/{confirmed.DocumentId}/file");
        file.StatusCode.Should().Be(HttpStatusCode.OK);
        (await file.Content.ReadAsByteArrayAsync()).Should().Equal(fileBytes);
    }

    [Fact]
    public async Task Revoking_a_document_soft_deletes_it()
    {
        var (token, _) = await RegisterAndLogin();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var initiate = await _client.PostAsJsonAsync("/api/v1/documents/upload-request",
            new { documentType = "ESG_REPORT", originalFileName = "esg.pdf", issuedAt = (string?)null, expiresAt = (string?)null });
        var uploadRequest = await initiate.Content.ReadFromJsonAsync<InitiateUploadResponse>();

        var confirm = await _client.PostAsync("/api/v1/documents/confirm-upload",
            BuildUploadBody(uploadRequest!.UploadRequestId, "esg report bytes"u8.ToArray()));
        var confirmed = await confirm.Content.ReadFromJsonAsync<ConfirmUploadResponse>();

        var revoke = await _client.DeleteAsync($"/api/v1/documents/{confirmed!.DocumentId}");
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterRevoke = await _client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{confirmed.DocumentId}");
        afterRevoke!.Status.Should().Be("Revoked");
    }

    [Fact]
    public async Task Confirming_an_upload_with_an_unknown_request_id_is_rejected()
    {
        var (token, _) = await RegisterAndLogin();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var confirm = await _client.PostAsync("/api/v1/documents/confirm-upload",
            BuildUploadBody(Guid.NewGuid(), "orphaned upload"u8.ToArray()));

        confirm.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
