using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class CreateInviteDtoValidator : AbstractValidator<CreateInviteDto>
{
    public CreateInviteDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}
