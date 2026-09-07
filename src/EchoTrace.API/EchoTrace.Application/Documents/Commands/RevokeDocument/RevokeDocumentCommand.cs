using MediatR;

namespace EchoTrace.Application.Documents.Commands.RevokeDocument;

public record RevokeDocumentCommand(Guid DocumentId) : IRequest;
