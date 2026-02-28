namespace EchoTrace.Domain.Exceptions;

public class DocumentIntegrityViolationException : Exception
{
    public Guid DocumentId { get; }

    public DocumentIntegrityViolationException(Guid documentId)
        : base($"Document {documentId} failed integrity check. The stored hash does not match the blob content.")
    {
        DocumentId = documentId;
    }
}
