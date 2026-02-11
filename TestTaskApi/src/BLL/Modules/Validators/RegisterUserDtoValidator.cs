using BLL.Dtos;
using FluentValidation;

namespace BLL.Modules.Validators;

public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long");

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters long");
        
        RuleFor(x => x.InviteCode)
            .NotEmpty()
            .WithMessage("Invite code is required");
        
        RuleFor(x => x.CaptchaId)
            .NotEmpty()
            .WithMessage("Captcha ID is required");

        RuleFor(x => x.CaptchaCode)
            .NotEmpty()
            .WithMessage("Captcha code is required");
    }
}
