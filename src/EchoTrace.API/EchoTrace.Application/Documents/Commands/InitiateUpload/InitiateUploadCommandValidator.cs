using FluentValidation;

namespace EchoTrace.Application.Documents.Commands.InitiateUpload;

public class InitiateUploadCommandValidator : AbstractValidator<InitiateUploadCommand>
{
    public InitiateUploadCommandValidator()
    {
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(500);

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(x => x.IssuedAt!.Value)
            .When(x => x.IssuedAt.HasValue && x.ExpiresAt.HasValue)
            .WithMessage("ExpiresAt must be after IssuedAt.");
    }
}
