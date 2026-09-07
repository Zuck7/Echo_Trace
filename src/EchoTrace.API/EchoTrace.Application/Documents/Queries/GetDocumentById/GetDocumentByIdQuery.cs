using EchoTrace.Application.Documents.Queries.GetDocuments;
using MediatR;

namespace EchoTrace.Application.Documents.Queries.GetDocumentById;

public record GetDocumentByIdQuery(Guid DocumentId) : IRequest<DocumentDto?>;
