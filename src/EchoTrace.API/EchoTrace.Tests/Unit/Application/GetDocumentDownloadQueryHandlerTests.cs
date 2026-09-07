using System.Security.Cryptography;
using System.Text;
using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Application.Documents.Queries.GetDocumentDownload;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Exceptions;
using EchoTrace.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;

namespace EchoTrace.Tests.Unit.Application;

public class GetDocumentDownloadQueryHandlerTests
{
    private readonly IDocumentRepository _documentRepo = Substitute.For<IDocumentRepository>();
    private readonly IFileServiceClient _fileService = Substitute.For<IFileServiceClient>();
    private readonly GetDocumentDownloadQueryHandler _sut;

    public GetDocumentDownloadQueryHandlerTests()
    {
        _sut = new GetDocumentDownloadQueryHandler(_documentRepo, _fileService);
    }

    private static (Document document, byte[] originalBytes) MakeDocument()
    {
        var bytes = Encoding.UTF8.GetBytes("this is the original, untampered certification content");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var document = Document.Create(
            Guid.NewGuid(), Guid.NewGuid(), "ISO_14001", "cert.pdf", "docs/cert.pdf",
            hash, bytes.LongLength, "application/pdf", null, null, Guid.NewGuid());

        return (document, bytes);
    }

    [Fact]
    public async Task Matching_hash_returns_a_verified_download_link()
    {
        var (document, bytes) = MakeDocument();
        _documentRepo.GetByIdAsync(document.DocumentId).Returns(document);
        _fileService.DownloadAsync(document.BlobPath).Returns(new MemoryStream(bytes));

        var result = await _sut.Handle(new GetDocumentDownloadQuery(document.DocumentId), CancellationToken.None);

        result.IntegrityVerified.Should().BeTrue();
        result.ContentHash.Should().Be(document.ContentHash);
        result.DownloadUrl.Should().Contain(document.DocumentId.ToString());
    }

    [Fact]
    public async Task Tampered_blob_content_is_detected_and_rejected()
    {
        var (document, _) = MakeDocument();
        var tamperedBytes = Encoding.UTF8.GetBytes("this content was swapped out after upload");

        _documentRepo.GetByIdAsync(document.DocumentId).Returns(document);
        _fileService.DownloadAsync(document.BlobPath).Returns(new MemoryStream(tamperedBytes));

        var act = () => _sut.Handle(new GetDocumentDownloadQuery(document.DocumentId), CancellationToken.None);

        await act.Should().ThrowAsync<DocumentIntegrityViolationException>();
    }

    [Fact]
    public async Task Unknown_document_id_throws_not_found()
    {
        _documentRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Document?)null);

        var act = () => _sut.Handle(new GetDocumentDownloadQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
