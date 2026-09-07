using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Documents.Commands.RevokeDocument;

public sealed class RevokeDocumentCommandHandler(
    IDocumentRepository documentRepo,
    IAuditService audit) : IRequestHandler<RevokeDocumentCommand>
{
    public async Task Handle(RevokeDocumentCommand request, CancellationToken ct)
    {
        var document = await documentRepo.GetByIdAsync(request.DocumentId, ct)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        document.Revoke();
        await documentRepo.UpdateAsync(document, ct);

        await audit.LogAsync("Document", document.DocumentId.ToString(), "DELETE", null,
            new { document.DocumentId, document.Status }, ct);
    }
}
