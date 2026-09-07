using FluentValidation;

namespace EchoTrace.Application.Documents.Commands.ConfirmUpload;

public class ConfirmUploadCommandValidator : AbstractValidator<ConfirmUploadCommand>
{
    public ConfirmUploadCommandValidator()
    {
        RuleFor(x => x.UploadRequestId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MimeType).NotEmpty();
    }
}
