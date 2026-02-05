using FluentValidation;

namespace Application.Users.Commands;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(255);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(255);
    }
}