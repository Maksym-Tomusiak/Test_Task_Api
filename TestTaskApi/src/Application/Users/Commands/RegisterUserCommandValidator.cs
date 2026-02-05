using FluentValidation;

namespace Application.Users.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3);
        
        RuleFor(x => x.CaptchaId)
            .NotEmpty();

        RuleFor(x => x.CaptchaCode)
            .NotEmpty();
    }
}