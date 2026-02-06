using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MaximumLength(255)
            .WithMessage("Username cannot exceed 255 characters");
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MaximumLength(255)
            .WithMessage("Password cannot exceed 255 characters");
    }
}
