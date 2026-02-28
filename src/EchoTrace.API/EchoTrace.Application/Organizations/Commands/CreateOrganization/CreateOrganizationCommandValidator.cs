using FluentValidation;

namespace EchoTrace.Application.Organizations.Commands.CreateOrganization;

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Country).NotEmpty().Length(2).WithMessage("Country must be a 2-letter ISO code.");
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
