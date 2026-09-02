using FluentValidation;

namespace EchoTrace.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.OrgLegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.OrgCountry).NotEmpty().Length(2)
            .WithMessage("OrgCountry must be a 2-letter ISO code.");
    }
}
